# 🎬 Movie Reservation API

Backend đặt vé xem phim: khách xem lịch chiếu, chọn ghế, giữ ghế 10 phút, thanh toán qua **VNPay sandbox**; admin quản lý phim / phòng / suất chiếu và xem báo cáo doanh thu.
Trọng tâm: **không bao giờ bán một ghế cho hai người**, kể cả khi nhiều request tới cùng một mili giây — và **chứng minh** điều đó bằng test tự động.

Đề bài: [roadmap.sh — Movie Reservation System](https://roadmap.sh/projects/movie-reservation-system).

**Demo:** `<link Render>` · Swagger: `<link Render>/swagger` · tài khoản khách demo `demo@movie.local` / `Demo@12345`

## Stack

.NET 10 · ASP.NET Core Web API · Clean Architecture · MediatR (CQRS) · FluentValidation · EF Core 10 + **PostgreSQL 17** · JWT (access + refresh token rotation) · VNPay (HMAC-SHA512) · xUnit + **Testcontainers** · Docker · Render

## Kiến trúc

```
Api ──► Application ──► Domain
 │           ▲
 └──► Infrastructure (EF Core, JWT, BCrypt, VNPay, background job)
```

- **Domain**: entity + luật thuần (`ShowtimeSeat.IsAvailableAt`, `Theater.GenerateSeats`…), không phụ thuộc gì.
- **Application**: mỗi use case một command/query + handler (MediatR) + validator. Chỉ khai báo thứ mình cần (`IAppDbContext`, `ITokenService`, `IVnPayGateway`), không biết HTTP.
- **Infrastructure**: cài đặt các interface đó. **Api**: controller mỏng (đọc `UserId` từ token rồi `sender.Send`), middleware đổi exception → ProblemDetails (400/401/404/409).
- Vì sao `IAppDbContext` thay vì Repository: [docs/decisions.md](docs/decisions.md) mục 3.

## ERD

```
User(Id, Email, PasswordHash, FullName, Role)
Theater(Id, Name) ─< Seat(Id, TheaterId, Row, Number)
Movie(Id, Title, DurationMinutes, Genre, SuitableAge, PosterUrl, IsActive)
Showtime(Id, MovieId, TheaterId, StartAt, EndAt, Price)
ShowtimeSeat(Id, ShowtimeId, SeatId, Status, ReservationId?, HeldUntil?, xmin)   -- "tồn kho" ghế của từng suất
Reservation(Id, UserId, ShowtimeId, Status, TotalPrice, CreatedAt, ExpiresAt, ConfirmedAt?, CancelledAt?)
ReservationSeat(Id, ReservationId, SeatId)                                        -- bản chụp ghế đã đặt
Payment(Id, ReservationId, TxnRef, Amount, Status, ProviderTransactionNo?, CreatedAt, CompletedAt?)
RefreshToken(Id, UserId, TokenHash, ExpiresAt, CreatedAt)
```

```
ĐƠN  Pending ──► Confirmed ──► Cancelled        GHẾ  Available ──► Held ──► Booked
        └──► Expired / Cancelled                         ▲__________|_________|  (hết hạn / huỷ)
```

## 🔒 How I prevented double-booking

**1. Tái hiện trước khi sửa.** Handler đặt vé ban đầu làm "đọc ghế → thấy trống → lưu" (*check-then-act*). Test [`DoubleBookingTests`](tests/MovieReservation.IntegrationTests/DoubleBookingTests.cs) bắn **20 request song song**, mỗi request một `DbContext` riêng như request thật, cùng đặt **một ghế** trên Postgres thật. Kết quả 3 lần chạy: **10/20, 20/20, 12/20** người cùng giữ được ghế đó — và không ai nhận lỗi. Bấm tay thử 1–2 lần không bao giờ thấy, vì khoảng hở chỉ vài mili giây.

**2. Chọn cách sửa** (so sánh đầy đủ ở [docs/concurrency.md](docs/concurrency.md)):

| | Optimistic (`xmin`) | Pessimistic (`FOR UPDATE`) | Atomic `UPDATE … WHERE` |
|---|---|---|---|
| Người thua | nhận lỗi ngay | phải xếp hàng chờ | nhận 0 dòng |
| Rủi ro | — | deadlock nếu khoá ngược thứ tự | luật ghế trống viết 2 nơi |

Đặt vé dùng **optimistic concurrency** với cột hệ thống `xmin` của Postgres: EF sinh `UPDATE … WHERE "Id" = @id AND xmin = @đã_đọc`; ai lưu sau sửa 0 dòng → `DbUpdateConcurrencyException` → **409**. Người thua không cần retry (ghế đã mất thật), cả đơn rollback (all-or-nothing nhờ một `SaveChanges`).

Các bước đổi trạng thái sau đó (**Confirm, Cancel, job hết hạn, IPN VNPay**) dùng **atomic conditional update**: điều kiện nằm trong `WHERE` (`Status = 'Pending' AND ExpiresAt > now`…), DB kiểm và sửa trong **một câu lệnh**, đếm số dòng để biết thắng hay thua. Nhờ vậy Confirm và job hết hạn chạy cùng lúc trên một đơn thì đơn chỉ có thể Confirmed **hoặc** Expired, và VNPay gọi IPN 10 lần chỉ xác nhận 1 lần.

**3. Chứng minh bằng test** — 20 request song song → **đúng 1** thành công, 19 nhận `ConflictException`; chạy 10 lần liên tiếp 10/10 xanh. Thêm: 2 khách đặt 2 tập ghế giao nhau một phần, Confirm sau hạn bị từ chối, ghế giữ quá 10 phút người khác đặt được (`FakeTimeProvider`, không chờ giờ thật), 10 IPN song song.

**Giữ ghế 10 phút** không phải khoá DB: là trạng thái `Held` + `HeldUntil`. Ghế quá hạn được coi là trống ngay (*lazy expiry*, `IsAvailableAt`), còn `BackgroundService` (`PeriodicTimer` 30s) dọn đơn quá hạn sang `Expired` cho dữ liệu sạch.

## Thanh toán VNPay

`POST /api/reservations/{id}/payments` → link VNPay có chữ ký (hạn thanh toán = hạn giữ ghế) → VNPay gọi IPN `GET /api/payments/vnpay/ipn` → kiểm **chữ ký HMAC-SHA512** trước mọi thứ, rồi TxnRef, số tiền, trạng thái → xác nhận đơn **idempotent**. Trả tiền khi đơn đã hết hạn → `RefundRequired`. Chi tiết và lý do: [docs/decisions.md](docs/decisions.md) mục 7.

Thẻ test sandbox: ngân hàng **NCB**, số thẻ `9704198526191432198`, tên `NGUYEN VAN A`, ngày phát hành `07/15`, OTP `123456`.

## Chạy local

Cần .NET SDK 10 và Docker Desktop.

```powershell
docker compose up -d                                   # Postgres 17
cd src/MovieReservation.Api
dotnet user-secrets set "Jwt:Key" "<chuỗi ngẫu nhiên ≥ 32 ký tự>"
dotnet user-secrets set "Admin:Password" "<mật khẩu admin>"
# Tuỳ chọn — thanh toán VNPay sandbox (đăng ký merchant test trên trang VNPay sandbox):
dotnet user-secrets set "VnPay:TmnCode" "<TmnCode>"
dotnet user-secrets set "VnPay:HashSecret" "<HashSecret>"
dotnet run --launch-profile http
```

Mở http://localhost:5161/swagger. Lúc khởi động app tự migrate DB, tạo admin `admin@movie.local` và dữ liệu demo (suất chiếu 7 ngày tới). Ở Development có `POST /api/reservations/{id}/confirm` (xác nhận giả) để thử luồng không cần VNPay.

**Frontend:** repo/folder riêng `movie-web` (Next.js) — `npm install && npm run dev` → http://localhost:3000, hướng dẫn trong README của folder đó.

**Thử VNPay ở local:** đặt vé trên FE → "Thanh toán qua VNPay" → trả bằng thẻ test → VNPay chuyển về `VnPay:ReturnUrl` (Development: `http://localhost:3000/payment/vnpay-return`) → trang FE chuyển nguyên query string sang `/api/payments/vnpay/return`, API kiểm chữ ký và xác nhận đơn.

```powershell
dotnet test      # Testcontainers tự bật Postgres trong Docker
```

## Deploy (Render)

Docker image từ `Dockerfile` (multi-stage). Biến môi trường:

| Biến | Giá trị |
|---|---|
| `ConnectionStrings__Default` | connection string Postgres managed |
| `Jwt__Key`, `Admin__Password` | bí mật |
| `VnPay__TmnCode`, `VnPay__HashSecret` | từ VNPay sandbox |
| `VnPay__ReturnUrl` | `https://<frontend>/payment/vnpay-return` |
| `Cors__AllowedOrigins__0` | domain frontend |
| `Seed__Demo`, `Seed__DemoUserPassword` | `true`, `Demo@12345` |
| `Swagger__Enabled` | `true` |

IPN URL khai báo trên cổng merchant VNPay: `https://<app>.onrender.com/api/payments/vnpay/ipn`.

## Phân quyền

Bảng đầy đủ: [docs/authorization.md](docs/authorization.md). Dữ liệu cá nhân kiểm chủ sở hữu trong handler, `UserId` chỉ lấy từ token; đơn của người khác trả **404** để không lộ đơn có tồn tại hay không (chống IDOR).
