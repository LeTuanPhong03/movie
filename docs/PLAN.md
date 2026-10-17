# 🎬 Movie Reservation — Lộ trình 1 tháng (17/10 → 15/11/2026)

> Bản rút gọn còn 30 ngày từ bản 6 tuần. Phần cốt lõi (chống double-booking, giữ ghế/hết hạn, deploy, README) giữ nguyên.
> Bản này giả định bạn làm khoảng **4–6 giờ/ngày**. Ít hơn mức đó thì gần như chắc chắn trễ, nên cắt bớt phạm vi sớm.

> 📓 Đang học tới đâu, nhật ký và câu phỏng vấn đã luyện: xem [NHAT-KY.md](NHAT-KY.md).

## Mục tiêu cuối cùng (15/11)

- [ ] API Movie Reservation chạy trên **link public** (có Swagger).
- [ ] **Chứng minh** bằng test tự động: N request đặt cùng 1 ghế thì chỉ đúng 1 request thành công.
- [ ] Repo public, README có mục **"How I prevented double-booking"**.
- [ ] CV có mô tả project và **đã gửi ít nhất 5 CV**.
- [ ] Trả lời trôi chảy các câu hỏi về Clean Architecture / Concurrency / CQRS / JWT.

## Đã cắt so với bản 6 tuần

Những phần dưới đây ghi vào mục "Future work" trong README. Nếu dư thời gian thì làm thêm.

- Báo cáo tỷ lệ lấp đầy (chỉ giữ báo cáo doanh thu)
- `PromoteToAdminCommand` (seed admin là đủ)
- GitHub Actions
- Loại ghế VIP/thường và giá theo loại
- Lọc/phân trang nâng cao (chỉ phân trang cơ bản)
- Số ngày buffer giảm từ 6 xuống **3** (CN 25/10, CN 1/11, CN 8/11)

## Quy tắc thực hiện

1. **Mỗi ngày ít nhất 1 commit.** Cuối ngày tick checkbox trong file này.
   Commit viết **tiếng Việt**, giữ tiền tố ngắn: `feat:` (tính năng), `fix:` (sửa lỗi), `test:`, `docs:`, `refactor:`, `chore:` (việc lặt vặt/cấu hình). Ví dụ: `feat: thêm enum trạng thái ghế`.
2. Mỗi ngày có một **"Xong khi"** (DoD). Chưa đạt thì chưa được tính là xong, và việc đó được chuyển sang buffer gần nhất.
3. **Trễ quá 1 ngày mà buffer đã dùng hết**: cắt theo danh sách [Có thể cắt tiếp](#-có-thể-cắt-tiếp-nếu-trễ), **không cắt test concurrency**.
4. Test dùng **DB thật (Postgres + Testcontainers)**, không dùng EF InMemory.
5. Code không gọi trực tiếp `DateTime.Now` hay `DateTime.UtcNow`, chỉ dùng `TimeProvider` được inject vào. Mọi thời gian lưu ở dạng **UTC**.
6. **Từ 26/10, mỗi tối dành 20 phút ôn phỏng vấn** về đúng phần code đã làm trong ngày: tự hỏi "vì sao làm vậy?" và trả lời thành tiếng. Tuần cuối khi đó chỉ còn là ôn lại.

## Stack đã chốt

.NET 10 · ASP.NET Core Web API · Clean Architecture · MediatR (CQRS) · FluentValidation · EF Core · **PostgreSQL** · JWT · xUnit + Testcontainers · Docker Compose · Deploy lên Render/Railway

## ERD định hướng (chốt ngày 17/10)

```
User(Id, Email, PasswordHash, Role)
Theater(Id, Name)
Seat(Id, TheaterId, Row, Number)                   -- ghế vật lý của phòng
Movie(Id, Title, Description, DurationMinutes, Genre, SuitableAge, PosterUrl)
Showtime(Id, MovieId, TheaterId, StartAt, EndAt, Price)
ShowtimeSeat(Id, ShowtimeId, SeatId, Status, ReservationId?, HeldUntil?, RowVersion)
                                                   -- "tồn kho" ghế của từng suất chiếu
Reservation(Id, UserId, ShowtimeId, Status[Pending|Confirmed|Cancelled|Expired],
            TotalPrice, CreatedAt, ExpiresAt, ConfirmedAt?, CancelledAt?)
```

- `ShowtimeSeat` được sinh tự động khi tạo Showtime. Mỗi dòng ứng với 1 ghế trong 1 suất chiếu, nên đây là chỗ để khóa hoặc đánh version.
- Unique `(ShowtimeId, SeatId)` trên `ShowtimeSeat`.
- Status và ExpiresAt **có từ đầu** để tuần concurrency không phải thiết kế lại.

### Hai bộ trạng thái (tra khi quên)

```
ĐƠN  (Reservation):  Pending → Confirmed → Cancelled   (khách tự hủy)
                         └──→ Expired                  (không trả tiền, hết 10 phút)

GHẾ  (ShowtimeSeat): Available → Held → Booked
                         ↑________|________|           (hết hạn hoặc hủy thì về Available)
```

> **Ghế** chỉ trả lời "ngồi được không?" → Available / Held / Booked. Bốn từ còn lại là của **đơn**.

---

## Giai đoạn 0 — Khởi động (17–18/10)

### T7 17/10 — Chốt ERD + hướng chống trùng ghế ✅ (xong sớm 16/10)
- [x] Chốt 7 bảng và quan hệ (Bài 1 với mentor)
- [x] Tự trả lời: hủy hoặc hết hạn thì ghế được nhả ra thế nào? (đặt `ShowtimeSeat.Status` về Available)
- [x] Viết `docs/decisions.md` **bằng lời của mình**: lý do tách Seat và ShowtimeSeat, lý do lưu TotalPrice
- **Xong khi:** ERD đủ 7 bảng, có khóa ngoại và unique index.

> Vì xong sớm, **17/10 làm luôn việc của 18/10** (dựng solution). Ngày 18/10 trở thành buffer hoặc bắt đầu sửa Auth của Project 1.

### CN 18/10 — Dựng solution + hạ tầng
- [x] Solution: `Domain`, `Application`, `Infrastructure`, `Api`, `UnitTests`, `IntegrationTests` (mentor dựng khung 16/10)
- [x] `docker-compose.yml` chạy Postgres
- [ ] EF Core DbContext + migration đầu tiên
- [ ] Integration test đầu tiên bằng Testcontainers: khởi động Postgres, chạy migration, assert bảng tồn tại
- [ ] `git init` → commit → push lên GitHub
- **Xong khi:** `docker compose up` và `dotnet test` đều chạy xanh.

---

## Tuần 1 (19–25/10) — Auth + Catalog + deploy sớm

### T2 19/10 — Auth
- [ ] Lấy lại Register/Login JWT từ Project 1
- [ ] Hai role `Admin` và `User`, seed 1 admin từ config (không hard-code password)
- [ ] Policy `AdminOnly`, Swagger có nút Authorize
- **Xong khi:** login admin lấy được token, gọi endpoint admin với token User thì nhận **403**.

### T3 20/10 — Theater + Movie
- [ ] `CreateTheaterCommand` kèm sơ đồ ghế (rows, seatsPerRow, tự sinh `Seat`)
- [ ] `Create/Update/DeleteMovieCommand`, `GetMoviesQuery` (phân trang cơ bản)
- [ ] Validator cho từng command
- **Xong khi:** CRUD Movie hoạt động và chỉ Admin ghi được.

### T4 21/10 — Showtime
- [ ] `CreateShowtimeCommand`: EndAt = StartAt + thời lượng phim + 15 phút dọn phòng
- [ ] **Từ chối nếu trùng giờ** với suất khác cùng phòng
- [ ] Trong cùng transaction, sinh `ShowtimeSeat` cho mọi ghế của phòng
- [ ] Integration test: số ShowtimeSeat bằng số ghế của phòng; suất trùng giờ thì bị từ chối
- **Xong khi:** 2 test trên pass.

### T5 22/10 — Truy vấn cho khách
- [ ] `GetShowtimesByDateQuery` (kèm thông tin phim, chỉ lấy suất chưa chiếu)
- [ ] `GetSeatsByShowtimeQuery` trả về sơ đồ ghế có trạng thái Available/Held/Booked
- [ ] Dùng `AsNoTracking` + projection, không trả entity ra ngoài
- **Xong khi:** gọi Swagger thấy sơ đồ ghế đúng.

### T6 23/10 — Deploy sớm
- [ ] Dockerfile cho API (multi-stage)
- [ ] Deploy lên Render/Railway cùng Postgres managed, migration tự chạy khi start
- [ ] Biến môi trường: connection string, JWT secret
- **Xong khi:** có link Swagger public mở được.

### T7 24/10 — Đặt vé (happy path)
- [ ] `CreateReservationCommand(showtimeId, seatIds[])`: kiểm tra ghế Available, tạo Reservation **Pending**, `ExpiresAt = now + 10 phút`, chuyển ghế sang Held
- [ ] Tính TotalPrice
- [ ] **Chưa** xử lý concurrency (cố ý để tuần sau tái hiện race)
- **Xong khi:** đặt 2 ghế thành công, DB đúng.

### CN 25/10 — Buffer #1
- [ ] Bù việc trễ của tuần 1. Nếu không trễ thì làm trước phần đọc của 27/10.

---

## Tuần 2 (26/10–1/11) — ⚔️ Concurrency + giữ ghế

### T2 26/10 — Tái hiện double-booking
- [ ] Integration test: 20 task song song, **mỗi task có scope/DbContext riêng**, cùng đặt 1 ghế
- [ ] Lưu bằng chứng (log + query DB) vào `docs/concurrency.md`
- **Xong khi:** có bằng chứng ít nhất 2 reservation cho cùng 1 ghế.

### T3 27/10 — Chọn hướng giải + implement
- [ ] So sánh 3 hướng: Optimistic (`xmin`/RowVersion), Pessimistic (`FOR UPDATE`), Atomic conditional update (`UPDATE ... WHERE Status='Available'` rồi kiểm tra số dòng bị ảnh hưởng)
- [ ] Viết ADR ngắn trong `docs/concurrency.md`
- [ ] Implement cách đã chọn, đặt nhiều ghế phải **all-or-nothing**
- **Xong khi:** 20 request song song chỉ có đúng **1** thành công.

### T4 28/10 — Test chắc chắn + lỗi chuẩn
- [ ] Assert: success == 1, các request còn lại nhận lỗi `SeatAlreadyReserved`
- [ ] Case 2 user đặt 2 tập ghế giao nhau một phần
- [ ] Chạy test 10 lần liên tiếp đều pass
- [ ] Middleware ProblemDetails: trùng ghế → 409, validation → 400, không có quyền → 403
- **Xong khi:** test ổn định, Swagger trả 409 đúng format.

### T5 29/10 — TimeProvider + lazy expiry
- [ ] Inject `TimeProvider` vào mọi handler
- [ ] Ghế Held có `HeldUntil < now` được coi là **Available**, cả khi xem ghế lẫn khi đặt
- **Xong khi:** test dùng `FakeTimeProvider` tua 11 phút thì ghế trống lại.

### T6 30/10 — Confirm
- [ ] `ConfirmReservationCommand` (mô phỏng thanh toán): chỉ chủ reservation được confirm
- [ ] Confirm **nguyên tử**: `WHERE Status='Pending' AND ExpiresAt > now`, ghế chuyển sang Booked
- **Xong khi:** test "confirm sau khi hết hạn" bị từ chối.

### T7 31/10 — Background service
- [ ] `BackgroundService` + `PeriodicTimer` chạy mỗi 30 giây: Pending quá hạn → Expired, nhả ghế
- [ ] Mỗi vòng quét tạo scope mới, log số reservation được xử lý
- **Xong khi:** chạy local, đặt vé, chờ hết hạn thì ghế tự nhả.

### CN 1/11 — Buffer #2
- [ ] Hoàn thiện concurrency nếu còn lỗi. Đây là ưu tiên số 1 của cả project.

---

## Tuần 3 (2/11–8/11) — Hủy vé, báo cáo, kiểm thử, deploy

### T2 2/11 — Hủy vé + lịch sử
- [ ] `CancelReservationCommand`: chỉ chủ vé hoặc Admin, **chỉ hủy suất chưa chiếu**, nhả ghế
- [ ] `GetMyReservationsQuery`: lọc theo status, kèm thông tin phim/suất/ghế
- **Xong khi:** user chỉ thấy và chỉ hủy được vé của mình.

### T3 3/11 — Test toàn luồng giữ ghế
- [ ] Đặt → hết hạn → ghế trống → người khác đặt được (FakeTimeProvider)
- [ ] Đặt → confirm → hủy → ghế trống
- [ ] Hủy suất đã chiếu hoặc vé sai trạng thái thì bị từ chối
- **Xong khi:** toàn bộ test xanh, không có test nào phải chờ thời gian thật.

### T4 4/11 — Báo cáo doanh thu + phân quyền
- [ ] `GetRevenueReportQuery(from, to, groupBy=day|movie)`: **chỉ tính Confirmed**, đối chiếu bằng SQL tay
- [ ] Bảng endpoint với quyền tương ứng trong `docs/authorization.md`
- [ ] Thử đổi id để xem vé người khác (IDOR) → phải nhận 403/404
- **Xong khi:** số liệu báo cáo khớp SQL, mọi endpoint đều có trong bảng.

### T5 5/11 — Integration test luồng chính
- [ ] Đăng ký → login → xem suất → xem ghế → đặt → confirm → xem lịch sử
- [ ] Admin: tạo phim → tạo suất → xem báo cáo
- [ ] Unit test cho logic chuyển trạng thái của Reservation và cách tính giá
- **Xong khi:** `dotnet test` xanh toàn bộ.

### T6 6/11 — Đối chiếu đề bài + deploy bản cuối
- [ ] Mở lại requirements gốc (roadmap.sh Movie Reservation) và tick từng ý
- [ ] Deploy lại, seed dữ liệu demo (3 phim, 2 phòng, các suất trong 7 ngày tới), tài khoản demo
- **Xong khi:** người lạ mở link là thử được ngay.

### T7 7/11 — README
- [ ] Giới thiệu, kiến trúc, ERD, hướng dẫn chạy local (`docker compose up`)
- [ ] **How I prevented double-booking**: race condition → bằng chứng → cách chọn → test chứng minh
- [ ] Known limitations / Future work (gồm các phần đã cắt ở trên)

### CN 8/11 — Buffer #3 (buffer cuối)

---

## Tuần 4 (9/11–15/11) — 🚀 CV, apply, ôn phỏng vấn

### T2 9/11 — Review code + GitHub
- [ ] Review cả 2 project: đặt lại tên, xóa code chết, không còn secret trong repo
- [ ] Repo public, pin 2 repo lên profile

### T3 10/11 — CV + apply
- [ ] Mỗi project 3–4 bullet có kỹ thuật và kết quả cụ thể
- [ ] Gửi **5 CV** đầu tiên, ghi lại vào `docs/applications.md` (file này không commit)

### T4 11/11 — Ôn Clean Architecture + CQRS/MediatR
- [ ] 10 câu mỗi chủ đề, trả lời thành tiếng, ghi câu bị vấp

### T5 12/11 — Ôn Concurrency + JWT/Auth
- [ ] 10 câu mỗi chủ đề, gắn với đúng code trong project

### T6 13/11 — Live-demo thử
- [ ] Demo cho bạn bè 15 phút: chạy test concurrency, giải thích từng bước

### T7 14/11 — Sửa lỗ hổng
- [ ] Sửa phần code hoặc phần giải thích bị vấp, gửi thêm CV

### CN 15/11 — Tổng kết 🎉
- [ ] Đối chiếu [Mục tiêu cuối cùng](#mục-tiêu-cuối-cùng-1511)
- [ ] Còn thời gian thì làm các phần đã cắt (báo cáo lấp đầy, GitHub Actions)

---

## ✂️ Có thể cắt tiếp nếu trễ

Cắt theo thứ tự từ trên xuống:
1. Báo cáo doanh thu theo `groupBy` (chỉ cần tổng theo khoảng thời gian)
2. Unit test domain (giữ integration test)
3. Deploy "sớm" ngày 23/10 (gộp vào 6/11, nhưng rủi ro cao hơn)

**Không bao giờ cắt:** test concurrency, giữ ghế/hết hạn, deploy public, README.

## 📈 Theo dõi tiến độ

| Giai đoạn | Hạn | Trạng thái |
|---|---|---|
| 0 — Khởi động | 18/10 | ⬜ |
| 1 — Auth + Catalog + Deploy | 25/10 | ⬜ |
| 2 — Concurrency + Giữ ghế | 01/11 | ⬜ |
| 3 — Hủy, Báo cáo, Test, README | 08/11 | ⬜ |
| 4 — CV + Apply + Phỏng vấn | 15/11 | ⬜ |
