using MediatR;

namespace MovieReservation.Application.Features.Auth.Logout;

/// <summary>
/// Đăng xuất thiết bị hiện tại: thu hồi refresh token (controller đọc từ cookie HttpOnly). Không cần access token.
/// </summary>
public record LogoutCommand(string RefreshToken) : IRequest;
