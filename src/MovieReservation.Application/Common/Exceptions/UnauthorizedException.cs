namespace MovieReservation.Application.Common.Exceptions;

/// <summary>
/// Lỗi xác thực: sai thông tin đăng nhập, refresh token không hợp lệ hoặc hết hạn.
/// Middleware sẽ đổi lỗi này thành HTTP 401 Unauthorized.
/// </summary>
public class UnauthorizedException(string message) : Exception(message);
