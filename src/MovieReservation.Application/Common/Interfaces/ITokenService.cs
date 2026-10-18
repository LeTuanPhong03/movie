using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Common.Interfaces;

public record AccessTokenResult(string AccessToken, DateTime ExpiresAt);
public record RefreshTokenResult(string RefreshToken, string RefreshTokenHash, DateTime ExpiresAt);

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user);
    RefreshTokenResult GenerateRefreshToken();
    string HashToken(string token);
}