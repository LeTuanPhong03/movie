using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Reservations.DTOs;

namespace MovieReservation.Application.Features.Reservations.GetAll;

/// <summary>
/// Danh sách đơn cho Admin, chạy 2 câu SELECT:
/// (1) lọc + đếm + lấy Id và thông tin người đặt của trang hiện tại (join Users);
/// (2) chiếu đúng các đơn đó qua ReservationProjection.ToDto — dùng lại phép chiếu của lịch sử đơn
///     thay vì viết lại (Expression không ghép được vào một Select khác), rồi xếp lại theo thứ tự của câu (1).
/// </summary>
public class GetAllReservationsQueryHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetAllReservationsQuery, PagedResult<AdminReservationDto>>
{
    public async Task<PagedResult<AdminReservationDto>> Handle(GetAllReservationsQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var query = db.Reservations.AsNoTracking().WhereStatus(request.Status, now);
        if (request.ShowtimeId.HasValue)
            query = query.Where(r => r.ShowtimeId == request.ShowtimeId.Value);
        if (request.UserId.HasValue)
            query = query.Where(r => r.UserId == request.UserId.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        // Join TRƯỚC khi sắp + cắt trang: join sau Skip/Take thì SQL không đảm bảo giữ thứ tự của câu con
        var page = await query
            .Join(db.Users, r => r.UserId, u => u.Id, (r, u) => new { r.Id, r.UserId, r.CreatedAt, u.Email, u.FullName })
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var ids = page.Select(x => x.Id).ToList();
        var dtos = await db.Reservations.AsNoTracking()
            .Where(r => ids.Contains(r.Id))
            .Select(ReservationProjection.ToDto(now))
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        var items = page
            .Select(x => new AdminReservationDto(x.UserId, x.Email, x.FullName, dtos[x.Id]))
            .ToList();
        return new PagedResult<AdminReservationDto>(items, request.Page, request.PageSize, totalCount);
    }
}
