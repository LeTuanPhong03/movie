using MediatR;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Auth.Refresh;

/// <summary>
/// Đổi refresh token (token gốc, controller đọc từ cookie HttpOnly) lấy cặp token mới. Không cần access token.
/// </summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
