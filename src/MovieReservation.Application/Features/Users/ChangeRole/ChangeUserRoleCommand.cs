using MediatR;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Users.ChangeRole;

/// <summary>
/// Admin đổi quyền một user (nâng lên Admin hoặc hạ về Customer). CurrentUserId do controller đọc từ token
/// để chặn Admin tự đổi quyền của chính mình (xem handler).
/// </summary>
public record ChangeUserRoleCommand(int UserId, UserRole Role, int CurrentUserId) : IRequest;
