using MediatR;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.GetAll;

/// <summary>
/// Admin xem tất cả đơn đặt (đề bài: Admin xem mọi reservation), lọc tuỳ chọn theo suất, user, trạng thái,
/// mới nhất trước, có phân trang. Mỗi dòng kèm email/họ tên người đặt để Admin biết đơn của ai.
/// </summary>
public record GetAllReservationsQuery(
    int? ShowtimeId = null,
    int? UserId = null,
    ReservationStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<AdminReservationDto>>
{
    public const int MaxPageSize = 100;
}

public record AdminReservationDto(int UserId, string UserEmail, string UserFullName, ReservationDto Reservation);
