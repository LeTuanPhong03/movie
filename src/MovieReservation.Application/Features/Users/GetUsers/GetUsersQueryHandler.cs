using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Users.GetUsers;

/// <summary>
/// Danh sách user cho Admin: lọc theo email/họ tên chứa chuỗi tìm kiếm, sắp theo Id, phân trang.
/// Select thẳng sang UserDto trong SQL → không tải PasswordHash lên bộ nhớ.
/// </summary>
public class GetUsersQueryHandler(IAppDbContext db) : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    public async Task<PagedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(u => u.Email.ToLower().Contains(search) || u.FullName.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, u.Role.ToString()))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserDto>(items, request.Page, request.PageSize, totalCount);
    }
}
