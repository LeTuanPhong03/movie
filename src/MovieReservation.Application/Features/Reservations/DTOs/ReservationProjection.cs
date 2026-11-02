using System.Linq.Expressions;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.DTOs;

/// <summary>
/// Phép chiếu Reservation → ReservationDto dùng chung cho lịch sử đơn và xem chi tiết đơn.
/// Là Expression (không phải hàm C# thường) để EF dịch được sang SQL: chỉ SELECT đúng cột cần, không tải cả entity.
/// Đơn Pending đã quá ExpiresAt hiển thị là Expired luôn (hết hạn lười), không chờ job dọn 30 giây.
/// Ghế lấy từ ReservationSeats (bản chụp) nên đơn đã huỷ/hết hạn vẫn thấy đủ ghế.
/// </summary>
public static class ReservationProjection
{
    public static Expression<Func<Reservation, ReservationDto>> ToDto(DateTime now) => r => new ReservationDto(
        r.Id,
        r.Status == ReservationStatus.Pending && r.ExpiresAt <= now ? ReservationStatus.Expired : r.Status,
        r.TotalPrice,
        r.ExpiresAt,
        r.ShowtimeId,
        r.Showtime.MovieId,
        r.Showtime.Movie.Title,
        r.Showtime.Theater.Name,
        r.Showtime.StartAt,
        r.Showtime.StartAt.AddMinutes(r.Showtime.Movie.DurationMinutes),
        r.Seats
            .OrderBy(s => s.Seat.Row).ThenBy(s => s.Seat.Number)
            .Select(s => new ReservedSeatDto(s.Seat.Row, s.Seat.Number))
            .ToList());

    // Lọc theo "trạng thái thật" khớp với ToDto: lọc Pending thì bỏ đơn đã quá hạn,
    // lọc Expired thì gồm cả đơn Pending quá hạn mà job chưa kịp dọn
    public static IQueryable<Reservation> WhereStatus(this IQueryable<Reservation> query, ReservationStatus? status, DateTime now) =>
        status switch
        {
            null => query,
            ReservationStatus.Pending => query.Where(r => r.Status == ReservationStatus.Pending && r.ExpiresAt > now),
            ReservationStatus.Expired => query.Where(r => r.Status == ReservationStatus.Expired
                                                     || (r.Status == ReservationStatus.Pending && r.ExpiresAt <= now)),
            var s => query.Where(r => r.Status == s)
        };
}
