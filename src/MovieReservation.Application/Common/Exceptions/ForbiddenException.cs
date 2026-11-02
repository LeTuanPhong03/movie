namespace MovieReservation.Application.Common.Exceptions;

/// <summary>
/// Đã đăng nhập, có đúng role, nhưng luật nghiệp vụ không cho làm việc này (ví dụ Admin thường hạ quyền Admin khác).
/// Middleware đổi thành HTTP 403 Forbidden. Khác 401: 401 là "chưa biết bạn là ai", 403 là "biết rồi nhưng không được".
/// </summary>
public class ForbiddenException(string message) : Exception(message);
