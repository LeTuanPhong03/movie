using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MovieReservation.Application.Features.Auth.DTOs;
using Testcontainers.PostgreSql;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Test luồng refresh token qua cookie HttpOnly bằng HTTP thật (WebApplicationFactory + Postgres):
/// đăng nhập đặt cookie đúng thuộc tính và body không lộ refresh token; refresh đọc cookie và xoay vòng
/// (cookie cũ dùng lại → 401); thiếu cookie → 401; logout xoá cookie và thu hồi token.
/// Client tự quản cookie bằng tay (HandleCookies = false) để gửi lại được cả cookie cũ đã bị xoay.
/// </summary>
public class AuthCookieTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseEnvironment("Development")
            .UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString())
            .UseSetting("Jwt:Key", "khoa-test-dai-it-nhat-32-ky-tu-cho-HS256-0123456789")
            .UseSetting("Admin:Email", "admin@test.local")
            .UseSetting("Admin:Password", "Admin@12345")
            .UseSetting("Seed:Demo", "false"));
        // https: cookie Secure; không tự quản cookie để test chủ động gửi cookie nào
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = false
        });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private Task<HttpResponseMessage> DangNhapAsync() =>
        _client.PostAsJsonAsync("/api/auth/login", new { email = "admin@test.local", password = "Admin@12345" });

    private Task<HttpResponseMessage> GuiKemCookieAsync(string path, string? refreshToken)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, path);
        if (refreshToken is not null) req.Headers.Add("Cookie", $"movie_refresh={refreshToken}");
        return _client.SendAsync(req);
    }

    private static string SetCookie(HttpResponseMessage res) =>
        Assert.Single(res.Headers.GetValues("Set-Cookie"), c => c.StartsWith("movie_refresh="));

    private static string GiaTriCookie(HttpResponseMessage res) =>
        SetCookie(res).Split(';')[0]["movie_refresh=".Length..];

    [Fact]
    public async Task Dang_nhap_dat_cookie_HttpOnly_va_body_khong_co_refresh_token()
    {
        var res = await DangNhapAsync();

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var cookie = SetCookie(res).ToLowerInvariant();
        Assert.Contains("httponly", cookie);
        Assert.Contains("secure", cookie);
        Assert.Contains("samesite=lax", cookie);
        Assert.Contains("path=/api/auth", cookie);

        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.TryGetProperty("refreshToken", out _));
        Assert.False(string.IsNullOrEmpty(body.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task Refresh_bang_cookie_cap_token_moi_va_cookie_cu_khong_dung_lai_duoc()
    {
        var cu = GiaTriCookie(await DangNhapAsync());

        var res = await GuiKemCookieAsync("/api/auth/refresh", cu);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var session = await res.Content.ReadFromJsonAsync<SessionResponse>();
        Assert.False(string.IsNullOrEmpty(session!.AccessToken));
        var moi = GiaTriCookie(res);
        Assert.NotEqual(cu, moi);

        // Rotation: token cũ đã bị xoá khỏi DB
        Assert.Equal(HttpStatusCode.Unauthorized, (await GuiKemCookieAsync("/api/auth/refresh", cu)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GuiKemCookieAsync("/api/auth/refresh", moi)).StatusCode);
    }

    [Fact]
    public async Task Refresh_khong_co_cookie_tra_401()
    {
        var res = await GuiKemCookieAsync("/api/auth/refresh", null);

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Logout_xoa_cookie_va_thu_hoi_refresh_token()
    {
        var token = GiaTriCookie(await DangNhapAsync());

        var res = await GuiKemCookieAsync("/api/auth/logout", token);

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        // Xoá cookie = cookie rỗng, hạn trong quá khứ, cùng Path
        var cookie = SetCookie(res).ToLowerInvariant();
        Assert.StartsWith("movie_refresh=;", cookie);
        Assert.Contains("expires=thu, 01 jan 1970", cookie);
        Assert.Contains("path=/api/auth", cookie);
        Assert.Equal(HttpStatusCode.Unauthorized, (await GuiKemCookieAsync("/api/auth/refresh", token)).StatusCode);
    }
}
