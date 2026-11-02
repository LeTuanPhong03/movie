using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using MovieReservation.Application.Common.Models;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Application.Features.Movies.DTOs;
using MovieReservation.Application.Features.Reports.Revenue;
using MovieReservation.Application.Features.Reservations.DTOs;
using MovieReservation.Application.Features.Showtimes.DTOs;
using MovieReservation.Application.Features.Theaters.DTOs;
using MovieReservation.Domain.Enums;
using Testcontainers.PostgreSql;

namespace MovieReservation.IntegrationTests;

/// <summary>
/// Test toàn luồng QUA HTTP (WebApplicationFactory chạy cả Program.cs: middleware, JWT, policy, ProblemDetails)
/// trên Postgres thật. Admin tạo phòng → phim → suất; khách đăng ký → xem suất → xem ghế → đặt → confirm → lịch sử;
/// khách khác dò đơn (IDOR) → 404; đặt trùng ghế → 409 ProblemDetails; báo cáo: Admin 200, khách 403, không token 401.
/// Dùng đồng hồ thật nên suất chiếu đặt ở 2 ngày sau.
/// </summary>
public class ApiEndToEndTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private WebApplicationFactory<Program> _factory = null!;

    private const string AdminEmail = "admin@test.local";
    private const string AdminPassword = "Admin@12345";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        // UseSetting (không phải ConfigureAppConfiguration) để Program.cs đọc được ngay từ dòng đầu, trước Build()
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseEnvironment("Development")
            .UseSetting("ConnectionStrings:Default", _postgres.GetConnectionString())
            .UseSetting("Jwt:Key", "khoa-test-dai-it-nhat-32-ky-tu-cho-HS256-0123456789")
            .UseSetting("Admin:Email", AdminEmail)
            .UseSetting("Admin:Password", AdminPassword)
            .UseSetting("Seed:Demo", "false")
            .UseSetting("Reservations:AllowManualConfirm", "true"));
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    private async Task<HttpClient> ClientAsync(string? email = null, string? password = null)
    {
        var client = _factory.CreateClient();
        if (email is not null)
        {
            var res = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
            res.EnsureSuccessStatusCode();
            var auth = await res.Content.ReadFromJsonAsync<AuthResponse>(Json);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        }
        return client;
    }

    private async Task<HttpClient> KhachMoiAsync(string email)
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/register",
            new { fullName = "Khach", email, password = "Khach@12345", confirmPassword = "Khach@12345" });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var auth = await res.Content.ReadFromJsonAsync<AuthResponse>(Json);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static async Task<T> DocAsync<T>(HttpResponseMessage res, HttpStatusCode expected)
    {
        Assert.Equal(expected, res.StatusCode);
        return (await res.Content.ReadFromJsonAsync<T>(Json))!;
    }

    [Fact]
    public async Task Toan_luong_dat_ve_va_phan_quyen()
    {
        // ── Admin dựng dữ liệu ──
        var admin = await ClientAsync(AdminEmail, AdminPassword);
        var theater = await DocAsync<TheaterDto>(
            await admin.PostAsJsonAsync("/api/theaters", new { name = "Phòng E2E", rows = 2, seatsPerRow = 4 }), HttpStatusCode.Created);
        var movie = await DocAsync<MovieDto>(
            await admin.PostAsJsonAsync("/api/movies", new { title = "Phim E2E", durationMinutes = 100, genre = "Drama", suitableAge = 0 }), HttpStatusCode.Created);
        var startAt = DateTime.UtcNow.Date.AddDays(2).AddHours(5); // 12:00 giờ VN, 2 ngày sau
        var showtime = await DocAsync<ShowtimeDto>(
            await admin.PostAsJsonAsync("/api/showtimes", new { movieId = movie.Id, theaterId = theater.Id, startAt, price = 75000 }), HttpStatusCode.Created);

        // ── Khách xem suất theo ngày VN và sơ đồ ghế (không cần đăng nhập) ──
        var anonymous = await ClientAsync();
        var ngayVn = DateOnly.FromDateTime(startAt.AddHours(7));
        var suat = await DocAsync<List<ShowtimeDto>>(await anonymous.GetAsync($"/api/showtimes?date={ngayVn:yyyy-MM-dd}"), HttpStatusCode.OK);
        Assert.Contains(suat, s => s.Id == showtime.Id && s.MovieEndAt == startAt.AddMinutes(100));
        var soDo = await DocAsync<SeatMapDto>(await anonymous.GetAsync($"/api/showtimes/{showtime.Id}/seats"), HttpStatusCode.OK);
        Assert.Equal(8, soDo.Seats.Count);
        var haiGhe = soDo.Seats.Take(2).Select(g => g.Id).ToList();

        // ── Khách A đặt 2 ghế → 201 + Location trỏ tới GET /api/reservations/{id} ──
        var khachA = await KhachMoiAsync("a@test.local");
        var datRes = await khachA.PostAsJsonAsync("/api/reservations", new { showtimeId = showtime.Id, seatIds = haiGhe });
        var don = await DocAsync<ReservationDto>(datRes, HttpStatusCode.Created);
        Assert.Equal($"/api/reservations/{don.Id}", datRes.Headers.Location!.AbsolutePath, ignoreCase: true);
        Assert.Equal(150000, don.TotalPrice);

        // ── Khách B đặt trùng 1 ghế → 409 dạng ProblemDetails ──
        var khachB = await KhachMoiAsync("b@test.local");
        var trung = await khachB.PostAsJsonAsync("/api/reservations", new { showtimeId = showtime.Id, seatIds = new[] { haiGhe[1] } });
        Assert.Equal(HttpStatusCode.Conflict, trung.StatusCode);
        Assert.Equal("application/problem+json", trung.Content.Headers.ContentType!.MediaType);

        // ── IDOR: B đổi Id trên URL → 404 cho cả xem, huỷ, confirm, tạo link thanh toán ──
        Assert.Equal(HttpStatusCode.NotFound, (await khachB.GetAsync($"/api/reservations/{don.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await khachB.PostAsync($"/api/reservations/{don.Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await khachB.PostAsync($"/api/reservations/{don.Id}/confirm", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await khachB.PostAsync($"/api/reservations/{don.Id}/payments", null)).StatusCode);

        // ── A confirm → 204; lịch sử của A có đơn Confirmed, của B rỗng ──
        Assert.Equal(HttpStatusCode.NoContent, (await khachA.PostAsync($"/api/reservations/{don.Id}/confirm", null)).StatusCode);
        var lichSuA = await DocAsync<PagedResult<ReservationDto>>(await khachA.GetAsync("/api/reservations?status=Confirmed"), HttpStatusCode.OK);
        Assert.Equal([don.Id], lichSuA.Items.Select(d => d.Id));
        Assert.Equal(ReservationStatus.Confirmed, lichSuA.Items[0].Status);
        var lichSuB = await DocAsync<PagedResult<ReservationDto>>(await khachB.GetAsync("/api/reservations"), HttpStatusCode.OK);
        Assert.Empty(lichSuB.Items);

        // ── Báo cáo: không token 401, khách 403, Admin thấy đúng 150.000đ ──
        var homNayVn = DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
        var urlBaoCao = $"/api/reports/revenue?from={homNayVn.AddDays(-1):yyyy-MM-dd}&to={homNayVn.AddDays(1):yyyy-MM-dd}";
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(urlBaoCao)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await khachA.GetAsync(urlBaoCao)).StatusCode);
        var baoCao = await DocAsync<RevenueReportDto>(await admin.GetAsync(urlBaoCao), HttpStatusCode.OK);
        Assert.Equal(150000, baoCao.TotalRevenue);
        Assert.Equal(2, baoCao.TotalTickets);

        // ── A huỷ → ghế trống lại, B đặt được ghế vừa nhả ──
        Assert.Equal(HttpStatusCode.NoContent, (await khachA.PostAsync($"/api/reservations/{don.Id}/cancel", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Created,
            (await khachB.PostAsJsonAsync("/api/reservations", new { showtimeId = showtime.Id, seatIds = new[] { haiGhe[1] } })).StatusCode);
    }

    [Fact]
    public async Task Hai_phim_trung_ten_thi_suat_chieu_va_don_tra_dung_MovieId()
    {
        // Tên phim không unique (ví dụ bản gốc và bản làm lại) → client phải nối suất ↔ phim bằng MovieId, không bằng tên
        var admin = await ClientAsync(AdminEmail, AdminPassword);
        var theater = await DocAsync<TheaterDto>(
            await admin.PostAsJsonAsync("/api/theaters", new { name = "Phòng trùng tên", rows = 1, seatsPerRow = 4 }), HttpStatusCode.Created);
        var banGoc = await DocAsync<MovieDto>(
            await admin.PostAsJsonAsync("/api/movies", new { title = "Vua Sư Tử", durationMinutes = 90, genre = "Hoạt hình", suitableAge = 0 }), HttpStatusCode.Created);
        var banLamLai = await DocAsync<MovieDto>(
            await admin.PostAsJsonAsync("/api/movies", new { title = "Vua Sư Tử", durationMinutes = 120, genre = "Phiêu lưu", suitableAge = 0 }), HttpStatusCode.Created);
        var startAt = DateTime.UtcNow.Date.AddDays(3).AddHours(2); // 09:00 giờ VN, 3 ngày sau
        var suatGoc = await DocAsync<ShowtimeDto>(
            await admin.PostAsJsonAsync("/api/showtimes", new { movieId = banGoc.Id, theaterId = theater.Id, startAt, price = 80000 }), HttpStatusCode.Created);
        var suatLamLai = await DocAsync<ShowtimeDto>(
            await admin.PostAsJsonAsync("/api/showtimes", new { movieId = banLamLai.Id, theaterId = theater.Id, startAt = startAt.AddHours(4), price = 80000 }), HttpStatusCode.Created);
        Assert.Equal(banGoc.Id, suatGoc.MovieId);
        Assert.Equal(banLamLai.Id, suatLamLai.MovieId);

        // Danh sách theo ngày và chi tiết suất: cùng Title nhưng MovieId khác nhau
        var anonymous = await ClientAsync();
        var ngayVn = DateOnly.FromDateTime(startAt.AddHours(7));
        var suat = await DocAsync<List<ShowtimeDto>>(await anonymous.GetAsync($"/api/showtimes?date={ngayVn:yyyy-MM-dd}"), HttpStatusCode.OK);
        Assert.Equal(banGoc.Id, suat.Single(s => s.Id == suatGoc.Id).MovieId);
        Assert.Equal(banLamLai.Id, suat.Single(s => s.Id == suatLamLai.Id).MovieId);
        var chiTiet = await DocAsync<ShowtimeDto>(await anonymous.GetAsync($"/api/showtimes/{suatLamLai.Id}"), HttpStatusCode.OK);
        Assert.Equal(banLamLai.Id, chiTiet.MovieId);

        // Đơn đặt suất bản làm lại mang MovieId bản làm lại — cả lúc tạo lẫn lúc xem lại (projection)
        var khach = await KhachMoiAsync("trungten@test.local");
        var soDo = await DocAsync<SeatMapDto>(await anonymous.GetAsync($"/api/showtimes/{suatLamLai.Id}/seats"), HttpStatusCode.OK);
        var don = await DocAsync<ReservationDto>(
            await khach.PostAsJsonAsync("/api/reservations", new { showtimeId = suatLamLai.Id, seatIds = new[] { soDo.Seats[0].Id } }), HttpStatusCode.Created);
        Assert.Equal(banLamLai.Id, don.MovieId);
        var xemLai = await DocAsync<ReservationDto>(await khach.GetAsync($"/api/reservations/{don.Id}"), HttpStatusCode.OK);
        Assert.Equal(banLamLai.Id, xemLai.MovieId);
    }

    [Fact]
    public async Task Admin_nang_quyen_user_va_cac_endpoint_quan_tri()
    {
        var admin = await ClientAsync(AdminEmail, AdminPassword);
        var khach = await KhachMoiAsync("nangquyen@test.local");

        // Khách gọi các endpoint quản trị → 403
        Assert.Equal(HttpStatusCode.Forbidden, (await khach.GetAsync("/api/users")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await khach.GetAsync("/api/admin/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await khach.GetAsync("/api/reports/capacity?from=2026-01-01&to=2026-01-02")).StatusCode);

        // Admin tìm user rồi nâng lên Admin → 204; role sai → 400; tự đổi mình → 409
        var tim = await DocAsync<PagedResult<UserDto>>(await admin.GetAsync("/api/users?search=nangquyen"), HttpStatusCode.OK);
        var userId = tim.Items.Single().Id;
        Assert.Equal(HttpStatusCode.BadRequest,
            (await admin.PutAsJsonAsync($"/api/users/{userId}/role", new { role = 99 })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PutAsJsonAsync($"/api/users/{userId}/role", new { role = "Admin" })).StatusCode);
        var adminId = (await DocAsync<PagedResult<UserDto>>(await admin.GetAsync($"/api/users?search={AdminEmail}"), HttpStatusCode.OK)).Items.Single().Id;
        Assert.Equal(HttpStatusCode.Conflict,
            (await admin.PutAsJsonAsync($"/api/users/{adminId}/role", new { role = "Customer" })).StatusCode);

        // Token cũ vẫn mang role Customer; đăng nhập lại mới có Admin
        Assert.Equal(HttpStatusCode.Forbidden, (await khach.GetAsync("/api/users")).StatusCode);
        var adminMoi = await ClientAsync("nangquyen@test.local", "Khach@12345");
        Assert.Equal(HttpStatusCode.OK, (await adminMoi.GetAsync("/api/users")).StatusCode);

        // Owner (admin từ cấu hình): Admin mới không hạ được Owner (409), không hạ được Admin khác (403),
        // nhưng nâng được khách; Owner hạ được Admin mới
        Assert.Equal(HttpStatusCode.Conflict,
            (await adminMoi.PutAsJsonAsync($"/api/users/{adminId}/role", new { role = "Customer" })).StatusCode);
        await KhachMoiAsync("khach2@test.local");
        var khach2Id = (await DocAsync<PagedResult<UserDto>>(await admin.GetAsync("/api/users?search=khach2"), HttpStatusCode.OK)).Items.Single().Id;
        Assert.Equal(HttpStatusCode.NoContent,
            (await adminMoi.PutAsJsonAsync($"/api/users/{khach2Id}/role", new { role = "Admin" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await adminMoi.PutAsJsonAsync($"/api/users/{khach2Id}/role", new { role = "Customer" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent,
            (await admin.PutAsJsonAsync($"/api/users/{userId}/role", new { role = "Customer" })).StatusCode);

        // POST phòng → Location trỏ tới GET /api/theaters/{id} có thật
        var tao = await admin.PostAsJsonAsync("/api/theaters", new { name = "Phòng Location", rows = 3, seatsPerRow = 5 });
        var phong = await DocAsync<TheaterDto>(tao, HttpStatusCode.Created);
        var xem = await DocAsync<TheaterDto>(await admin.GetAsync(tao.Headers.Location), HttpStatusCode.OK);
        Assert.Equal(phong, xem);
    }

    [Fact]
    public async Task Validation_tra_400_ProblemDetails_co_danh_sach_loi()
    {
        var khach = await KhachMoiAsync("c@test.local");

        var res = await khach.GetAsync("/api/reservations?page=0&pageSize=1000");

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        using var body = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("Page", out _));
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("PageSize", out _));
    }
}
