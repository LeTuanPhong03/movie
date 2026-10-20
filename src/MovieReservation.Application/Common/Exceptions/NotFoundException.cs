namespace MovieReservation.Application.Common.Exceptions;

/// <summary>
/// Lỗi không tìm thấy tài nguyên (ví dụ phim theo Id không tồn tại hoặc đã bị ẩn).
/// Middleware sẽ đổi lỗi này thành HTTP 404 Not Found.
/// </summary>
public class NotFoundException(string message) : Exception(message);
