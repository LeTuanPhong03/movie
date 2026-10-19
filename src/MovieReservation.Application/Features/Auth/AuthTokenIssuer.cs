using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Auth;

/// <summary>
/// Gom phần cấp token dùng chung cho Register, Login và Refresh.
/// Cách dùng: AddRefreshToken (chưa lưu) → SaveChangesAsync → CreateResponse.
/// Tách 2 bước vì Register phải lưu xong mới có user.Id để sinh access token.
/// </summary>
public class AuthTokenIssuer(IAppDbContext db, ITokenService tokenService, TimeProvider timeProvider)
{
    /// <summary>
    /// Sinh refresh token mới, thêm dòng RefreshToken (chỉ lưu hash) vào DbContext — chưa SaveChanges.
    /// Trả về token gốc để gửi cho client.
    /// </summary>
    public string AddRefreshToken(User user)
    {
        var result = tokenService.GenerateRefreshToken();
        db.RefreshTokens.Add(new RefreshToken
        {
            User = user,
            TokenHash = result.RefreshTokenHash,
            ExpiresAt = result.ExpiresAt,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        });
        return result.RefreshToken;
    }

    /// <summary>
    /// Sinh access token và đóng gói AuthResponse. Gọi SAU SaveChanges (cần user.Id).
    /// </summary>
    public AuthResponse CreateResponse(User user, string refreshToken)
    {
        var accessToken = tokenService.GenerateAccessToken(user);
        return new AuthResponse(
            User: new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString()),
            AccessToken: accessToken.AccessToken,
            RefreshToken: refreshToken,
            AccessTokenExpiresAt: accessToken.ExpiresAt);
    }
}
