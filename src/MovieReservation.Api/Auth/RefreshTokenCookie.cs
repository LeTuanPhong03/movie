namespace MovieReservation.Api.Auth;

/// <summary>
/// Đọc/ghi refresh token trong cookie HttpOnly thay vì trả trong body JSON.
/// HttpOnly: JavaScript (kể cả script độc do XSS chèn vào) không đọc được → không lấy trộm được phiên dài hạn.
/// Secure: chỉ gửi qua HTTPS (trình duyệt coi http://localhost là an toàn nên chạy local vẫn được).
/// SameSite=Lax: trang web khác gửi POST sang thì trình duyệt không đính kèm cookie → chặn CSRF cho refresh/logout.
/// Path=/api/auth: cookie chỉ đi theo request refresh/logout, các API khác không mang theo.
/// </summary>
public static class RefreshTokenCookie
{
    public const string Name = "movie_refresh";
    private const string Path = "/api/auth";

    public static string? Read(HttpRequest request) => request.Cookies[Name];

    public static void Append(HttpResponse response, string refreshToken, int days) =>
        response.Cookies.Append(Name, refreshToken, Options(DateTimeOffset.UtcNow.AddDays(days)));

    // Xoá cookie = gửi lại cùng tên + cùng Path với hạn trong quá khứ; khác Path thì trình duyệt coi là cookie khác
    public static void Delete(HttpResponse response) => response.Cookies.Delete(Name, Options(null));

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = Path,
        Expires = expires,
        IsEssential = true
    };
}
