namespace MovieReservation.Application.Common.Extensions;

/// <summary>
/// Extension method cho chuỗi dùng khi chuẩn hóa input: trường không bắt buộc mà gửi rỗng hoặc toàn dấu cách thì lưu null.
/// </summary>
public static class StringExtensions
{
    public static string? NullIfBlank(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
