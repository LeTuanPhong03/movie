using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Common.Interfaces;

public record AccessTokenResult(string AccessToken, DateTime ExpiresAt);
public record RefreshTokenResult(string RefreshToken, string RefreshTokenHash, DateTime ExpiresAt);
/// <summary>
/// Hợp đồng sinh access token, refresh token và băm token cho luồng xác thực.
/// Cài đặt nằm ở Infrastructure (JwtTokenService).
/// </summary>
public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);
    RefreshTokenResult GenerateRefreshToken();
    string HashToken(string token);
}