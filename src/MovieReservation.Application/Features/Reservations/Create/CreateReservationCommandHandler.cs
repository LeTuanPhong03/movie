using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.Create;

/// <summary>
/// Đặt vé (happy path): kiểm tra suất còn đặt được, ghế thuộc suất và còn trống (tính cả Held đã hết hạn),
/// rồi tạo Reservation Pending + chuyển ghế sang Held trong MỘT lần SaveChanges (tất cả hoặc không gì cả).
/// CHƯA chống đặt trùng khi 2 người bấm cùng lúc — cố ý để tuần 2 tái hiện race condition.
/// </summary>
public class CreateReservationCommandHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<CreateReservationCommand, ReservationDto>
{
    public async Task<ReservationDto> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        // Cần dùng dữ liệu suất (giá, giờ, tên phim/phòng) → FirstOrDefaultAsync chứ không phải AnyAsync
        var showtime = await db.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Theater)
            .FirstOrDefaultAsync(s => s.Id == request.ShowtimeId, cancellationToken);
        if (showtime == null)
        {
            throw new NotFoundException($"Không tìm thấy suất chiếu có Id {request.ShowtimeId}.");
        }
        if (showtime.StartAt <= now)
        {
            throw new ConflictException("Suất chiếu đã bắt đầu, không thể đặt vé.");
        }

        // Lọc theo cả ShowtimeId → Id ghế của suất khác sẽ không tìm thấy
        var seats = await db.ShowtimeSeats
            .Include(ss => ss.Seat)
            .Where(ss => ss.ShowtimeId == request.ShowtimeId && request.SeatIds.Contains(ss.Id))
            .ToListAsync(cancellationToken);
        if (seats.Count != request.SeatIds.Count)
        {
            var missing = request.SeatIds.Except(seats.Select(ss => ss.Id));
            throw new NotFoundException($"Ghế không thuộc suất chiếu này: {string.Join(", ", missing)}.");
        }

        // Một ghế không trống là từ chối cả đơn
        var taken = seats.Where(ss => !ss.IsAvailableAt(now)).ToList();
        if (taken.Count > 0)
        {
            var names = string.Join(", ", taken.Select(ss => $"{ss.Seat.Row}{ss.Seat.Number}"));
            throw new ConflictException($"Ghế đã có người giữ hoặc đã bán: {names}.");
        }

        var reservation = new Reservation
        {
            UserId = request.UserId,
            ShowtimeId = showtime.Id,
            Status = ReservationStatus.Pending,
            TotalPrice = showtime.Price * seats.Count,
            CreatedAt = now,
            ExpiresAt = now.AddMinutes(Reservation.HoldMinutes)
        };
        db.Reservations.Add(reservation);

        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Held;
            seat.HeldUntil = reservation.ExpiresAt;
            // Gán navigation thay vì ReservationId: lúc này đơn chưa có Id, EF sẽ điền khi lưu
            seat.Reservation = reservation;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new ReservationDto(
            reservation.Id,
            reservation.Status,
            reservation.TotalPrice,
            reservation.ExpiresAt,
            showtime.Id,
            showtime.Movie.Title,
            showtime.Theater.Name,
            showtime.StartAt,
            seats.OrderBy(ss => ss.Seat.Row).ThenBy(ss => ss.Seat.Number)
                .Select(ss => new ReservedSeatDto(ss.Seat.Row, ss.Seat.Number))
                .ToList());
    }
}
