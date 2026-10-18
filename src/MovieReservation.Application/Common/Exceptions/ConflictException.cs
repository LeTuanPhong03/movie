namespace MovieReservation.Application.Common.Exceptions;

/// <summary>
/// Lỗi xung đột dữ liệu (ví dụ email đã tồn tại, ghế đã có người giữ).
/// Middleware sẽ đổi lỗi này thành HTTP 409 Conflict.
/// </summary>
public class ConflictException(string message) : Exception(message);
