using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Reservations.GetMine;

/// <summary>
/// Lịch sử đơn của một user. Bộ lọc trạng thái tính theo "trạng thái thật" (ReservationProjection.WhereStatus):
/// lọc Pending thì bỏ đơn đã quá hạn, lọc Expired thì gồm cả đơn Pending quá hạn mà job chưa kịp dọn.
/// </summary>
public class GetMyReservationsQueryHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetMyReservationsQuery, PagedResult<ReservationDto>>
{
    public async Task<PagedResult<ReservationDto>> Handle(GetMyReservationsQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Reservations.AsNoTracking()
            .Where(r => r.UserId == request.UserId)
            .WhereStatus(request.Status, now);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(ReservationProjection.ToDto(now))
            .ToListAsync(cancellationToken);

        return new PagedResult<ReservationDto>(items, request.Page, request.PageSize, totalCount);
    }
}
