using MediatR;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Auth.Refresh;

/// <summary>
/// Đổi refresh token (token gốc client đang giữ) lấy cặp token mới. Không cần access token.
/// </summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResponse>;
