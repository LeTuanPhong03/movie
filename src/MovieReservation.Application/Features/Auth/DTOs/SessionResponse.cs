namespace MovieReservation.Application.Features.Auth.DTOs;

/// <summary>
/// Body JSON trả cho client sau đăng ký / đăng nhập / làm mới token: thông tin người dùng + access token.
/// Cố ý KHÔNG có refresh token — nó nằm trong cookie HttpOnly để JavaScript không đọc được.
/// </summary>
public record SessionResponse(UserDto User, string AccessToken, DateTime AccessTokenExpiresAt)
{
    public static SessionResponse From(AuthResponse auth) => new(auth.User, auth.AccessToken, auth.AccessTokenExpiresAt);
}
