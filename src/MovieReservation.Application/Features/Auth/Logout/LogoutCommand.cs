using MediatR;

namespace MovieReservation.Application.Features.Auth.Logout;

/// <summary>
/// Đăng xuất thiết bị hiện tại: thu hồi refresh token client đang giữ. Không cần access token.
/// </summary>
public record LogoutCommand(string RefreshToken) : IRequest;
