using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Common.Models;

namespace MovieReservation.Application.Features.Refunds.GetAll;

/// <summary>
/// Danh sách khoản hoàn tiền: join Payments → Reservations → Users ngay trong SQL (Payment không có navigation),
/// sắp mới nhất trước theo lúc thu tiền rồi Id, phân trang.
/// </summary>
public class GetRefundsQueryHandler(IAppDbContext db) : IRequestHandler<GetRefundsQuery, PagedResult<RefundDto>>
{
    public async Task<PagedResult<RefundDto>> Handle(GetRefundsQuery request, CancellationToken cancellationToken)
    {
        var query =
            from p in db.Payments.AsNoTracking()
            where p.Status == request.Status
            join r in db.Reservations on p.ReservationId equals r.Id
            join u in db.Users on r.UserId equals u.Id
            select new { p, r, u };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.p.CompletedAt)
            .ThenByDescending(x => x.p.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new RefundDto(
                x.p.Id,
                x.r.Id,
                x.r.Status,
                x.u.Email,
                x.u.FullName,
                x.r.Showtime.Movie.Title,
                x.r.Showtime.StartAt,
                x.p.Amount,
                x.p.TxnRef,
                x.p.ProviderTransactionNo,
                x.p.Status,
                x.p.CompletedAt,
                x.p.RefundedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<RefundDto>(items, request.Page, request.PageSize, totalCount);
    }
}
