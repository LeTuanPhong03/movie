using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.Create;

/// <summary>
/// Đặt vé: kiểm tra suất còn đặt được, ghế thuộc suất và còn trống (tính cả Held đã hết hạn),
/// rồi tạo Reservation Pending + chuyển ghế sang Held trong MỘT lần SaveChanges (tất cả hoặc không gì cả).
/// Chống đặt trùng bằng optimistic concurrency (RowVersion ánh xạ xmin): nếu ghế bị người khác giữ
/// trong lúc đang đặt, người lưu sau nhận 409 và không ghế nào bị giữ nửa chừng.
/// </summary>

public class CreateReservationCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CreateReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        DateTime now = timeProvider.GetUtcNow().UtcDateTime;
        // 1. Kiểm tra suất còn đặt được
        var showtime = await db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Theater)
            .FirstOrDefaultAsync(s => s.Id == request.ShowtimeId, cancellationToken);
        if (showtime is null)
            throw new NotFoundException($"Không tìm thấy suất chiếu {request.ShowtimeId}");
        if(showtime.StartAt <= now)
            throw new ConflictException("Suất chiếu đã bắt đầu, không thể đặt vé.");
        // 2. Kiểm tra ghế thuộc suất
        var showtimeSeats = await db.ShowtimeSeats
            .Include(ss => ss.Seat)
            .Where(ss => ss.ShowtimeId == request.ShowtimeId && request.SeatIds.Contains(ss.Id))
            .ToListAsync(cancellationToken);
        if (showtimeSeats.Count != request.SeatIds.Count)
        {
            var missingSeatIds = request.SeatIds.Except(showtimeSeats.Select(ss => ss.Id)).ToList();
            throw new NotFoundException($"Ghế {string.Join(", ", missingSeatIds)} không thuộc suất chiếu {request.ShowtimeId}.");
        }
        // 3. Kiểm tra xem ghế đã bị đặt hoặc đang giữ không
        var gheDaDat = showtimeSeats.Where(ss => !ss.IsAvailableAt(now)).ToList();
        if(gheDaDat.Any())
        {
            throw new ConflictException($"Ghế {string.Join(", ", gheDaDat.Select(ss => $"{ss.Seat.Row}{ss.Seat.Number}"))} đã được đặt hoặc đang giữ.");
        }
        // 4. Tạo Reservation Pending + chuyển ghế sang Held
        var reservation = new Reservation
        {
            UserId = request.UserId,
            ShowtimeId = request.ShowtimeId,
            Status = ReservationStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(Reservation.HoldMinutes),
            TotalPrice = showtime.Price * showtimeSeats.Count,
        };
        db.Reservations.Add(reservation);
        foreach (var showtimeSeat in showtimeSeats)
        {
            showtimeSeat.Status = SeatStatus.Held;
            // Gắn Reservation vào ShowtimeSeat để biết ghế đang giữ cho đơn nào, và khi nào hết hạn giữ
            showtimeSeat.Reservation = reservation;
            showtimeSeat.HeldUntil = reservation.ExpiresAt;
        }
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Có người giữ ghế trước (xmin đã đổi). SaveChanges đã tự rollback cả đơn
            // không retry vì ghế chỉ có một: thua tức là ghế đã mất.
            throw new ConflictException("Ghế đã được đặt hoặc đang giữ bởi người khác. Vui lòng chọn ghế khác.");
        }
        return new ReservationDto
        (
            Id: reservation.Id,
            ShowtimeId: reservation.ShowtimeId,
            Status: reservation.Status,
            ExpiresAt: reservation.ExpiresAt,
            TotalPrice: reservation.TotalPrice,
            MovieTitle: showtime.Movie.Title,
            TheaterName: showtime.Theater.Name,
            StartAt: showtime.StartAt,
            MovieEndAt: showtime.StartAt.AddMinutes(showtime.Movie.DurationMinutes),
            Seats: showtimeSeats.OrderBy(ss => ss.Seat.Row).ThenBy(ss => ss.Seat.Number).Select(ss => new ReservedSeatDto
            (
                ss.Seat.Row,
                ss.Seat.Number
            )).ToList()
        );
    }
}