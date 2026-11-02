using MediatR;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Users.GetUsers;

/// <summary>
/// Admin tìm user (theo email hoặc họ tên, không phân biệt hoa/thường) để chọn người cần đổi quyền.
/// Kết quả là UserDto nên không bao giờ lộ PasswordHash.
/// </summary>
public record GetUsersQuery(string? Search = null, int Page = 1, int PageSize = 20) : IRequest<PagedResult<UserDto>>
{
    public const int MaxPageSize = 50;
}
