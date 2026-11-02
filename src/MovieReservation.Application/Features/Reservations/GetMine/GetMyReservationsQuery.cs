using MediatR;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.GetMine;

/// <summary>
/// Lịch sử đơn của chính người đang đăng nhập, mới nhất trước, có phân trang và lọc theo trạng thái.
/// UserId do controller đọc từ token → không có tham số nào cho phép xem đơn của người khác (chống IDOR từ gốc).
/// </summary>
public record GetMyReservationsQuery(int UserId, ReservationStatus? Status = null, int Page = 1, int PageSize = 10)
    : IRequest<PagedResult<ReservationDto>>
{
    public const int MaxPageSize = 50;
}
