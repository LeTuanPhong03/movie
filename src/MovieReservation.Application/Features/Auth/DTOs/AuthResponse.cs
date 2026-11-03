namespace MovieReservation.Application.Features.Auth.DTOs;

/// <summary>
/// Kết quả chung của đăng ký, đăng nhập và làm mới token: thông tin người dùng,
/// access token (JWT), refresh token và thời điểm access token hết hạn (UTC).
/// Chỉ dùng giữa handler và controller: controller đưa refresh token vào cookie HttpOnly, body trả SessionResponse.
/// </summary>
public record AuthResponse(
    UserDto User,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt);
