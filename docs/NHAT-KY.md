# 📓 Nhật ký học — Movie Reservation

> File này cập nhật **sau mỗi buổi học**. Mở project ở phiên mới thì đọc mục **Hôm nay học gì** trước.
> Lộ trình tổng thể và các ngày đã tick nằm ở [PLAN.md](PLAN.md).

## 📍 Hôm nay học gì

**Cập nhật lần cuối:** 17/10/2026

**Bước tiếp theo:**
1. **Bài 8 — Auth (việc của 19/10):** lấy lại Register/Login JWT từ Project 1, role `Admin`/`Customer`, seed admin từ config, policy `AdminOnly`, nút Authorize trên Swagger
2. **Đầu buổi:** ôn các câu ⚠️, ưu tiên: OnDelete (SetNull vs Restrict) và "vì sao vẫn cần unique khi code đã kiểm tra" (race condition)

## 🔁 Cách ôn câu phỏng vấn

- Kho câu hỏi ở cuối file **không phải để trả lời hết ngay**, mà dùng để ôn dần.
- Cuối mỗi buổi, mentor hỏi **2–3 câu ngẫu nhiên** (ưu tiên câu cũ). Trả lời bằng lời của mình, không nhìn tài liệu.
- Câu trả lời sai hoặc ấp úng thì đánh dấu ⚠️ cạnh câu đó để hỏi lại hôm sau. Trả lời tốt 2 lần liền thì đánh dấu ✅.
- Gợi ý trong ngoặc chỉ để tự kiểm tra, **không học thuộc**.
- 17/10: ôn lần đầu cuối buổi. 3 câu: index leftmost prefix đúng; OnDelete và unique-vs-race còn thiếu ý (⚠️).

## 📅 Nhật ký

| Ngày | Bài | Đã học / đã làm | Còn yếu |
|---|---|---|---|
| 16/10 | Bài 1: ERD | 7 bảng và quan hệ; tách Seat/ShowtimeSeat; TotalPrice là snapshot; vòng đời ghế; viết `docs/decisions.md` | Hay nhầm tên trạng thái của **đơn** với của **ghế** |
| 16/10 | Dựng khung | Mentor tạo solution 6 project và thư mục; hiểu dependency rule (Api → Application → Domain); commit đầu tiên | |
| 17/10 | Bài 2: Enum | Viết `SeatStatus` và `ReservationStatus`; hiểu enum là gì; bài học "build không lỗi chưa chắc code đúng" | Lần đầu để `Expired` nhầm sang enum ghế (đã sửa); gõ sai tên `RevervationStatus` (đã sửa, commit `93e75be`) |
| 17/10 | Ôn ERD | Hiểu `ConfirmedAt`/`CancelledAt` lưu "lúc nào"; đơn Expired thì cả hai `null`; `ExpiresAt` đủ, không cần `ExpiredAt` | Trả lời đúng ngay lần đầu, nhầm lẫn trạng thái đã giảm |
| 17/10 | Bài 3: Entity | Tự viết đủ 7 entity; dùng `decimal` cho tiền, enum cho Status, `?` đúng chỗ. Học: khóa ngoại khác navigation property | Đặt tên chưa thống nhất (`ShowTime`/`Showtime`), lỗi chính tả, `Role` để string thay vì enum, `DurationMinutes` dùng `TimeSpan` |
| 17/10 | Bài 3: sửa review | Sửa 6/7 điểm; tự chọn tên `UserRole.Customer` (tốt hơn gợi ý); học warning CS8618 và cách `= string.Empty` | Sót `ShowTimeId` ở Reservation; dùng Ctrl+Shift+F để rà tên |
| 17/10 | Bài 3: xong ✅ | Build sạch 0 warning; `Genre` chọn `string` (cách A); commit `3b3cfcc` | Từng đổi `Genre` sang `int` để tắt cảnh báo → bài học: sửa cho đúng nghĩa, không sửa cho hết cảnh báo |
| 17/10 | Bài 4: Docker | Hiểu image / container / volume (laptop và ổ cứng gắn ngoài); viết `docker-compose.yml`; Postgres 17 chạy được | Từng nghĩ volume là "cache/backup", thật ra là nơi lưu dữ liệu thật, không phải bản sao |
| 17/10 | Bài 4: xong ✅ | Tự kiểm chứng volume giữ dữ liệu sau `down`/`up`; commit `cb728c1` | |
| 17/10 | Bài 5 phần 1 | Hiểu ORM / DbContext / DbSet / migration; cài EF Core 10 + Npgsql + `dotnet ef`; tự viết `AppDbContext` đủ 7 DbSet | Đặt tên kệ `ReservationSeats` cho `ShowtimeSeat` (tên DbSet = tên bảng) |
| 17/10 | Bài 5 phần 2: xong ✅ | Connection string, `AddDbContext` (DI), migration `InitialCreate`, 7 bảng trong Postgres; tự giải thích đúng `__EFMigrationsHistory` | Phát hiện qua `\d`: **chưa có khóa ngoại, chưa có unique index**, Status lưu dạng số → học ở Bài 6 |
| 17/10 | Commit Bài 5 | Commit `8fe5ded` | |
| 17/10 | Bài 6: Fluent API | Hiểu configuration = "nội quy" của bảng, đặt ở Infrastructure để Domain sạch; tự viết `ShowtimeSeatConfiguration` (lưu Status dạng chữ, 3 khóa ngoại, unique `(ShowtimeId, SeatId)`); `ApplyConfigurationsFromAssembly`; hiểu `virtual` / `override` / `base` | Unique chỉ `SeatId` sẽ làm ghế không bán được cho suất khác (không phải "ghi đè") |
| 17/10 | Bài 6: warning NuGet | Sửa MSB3277 (2 phiên bản EF Core) bằng cách ghi rõ EF Core 10.0.13 trong Infrastructure; build 0 warning | |
| 17/10 | Bài 6: config còn lại | Mentor viết 6 config còn lại theo yêu cầu. Học `OnDelete`: Cascade / Restrict / SetNull và vì sao Reservation dùng Restrict (giữ lịch sử doanh thu) | Gắn SetNull nhầm vào bảng Reservation (đúng là ở `ShowtimeSeat.ReservationId`) ⚠️ |
| 17/10 | Bài 6: index | Index = mục lục đã sắp xếp; đọc nhanh, ghi chậm; unique = index + luật; unique mạnh khi luật là "bằng nhau", luật khoảng thời gian (chồng lấn suất chiếu) phải kiểm tra bằng code. Tự quyết **không** unique `Showtimes(TheaterId, StartAt)`. Hiểu **leftmost prefix**, giải thích được vì sao EF không tạo index riêng cho `ShowtimeId` | Lúc đầu nói index `(TheaterId, StartAt)` giúp được `WHERE StartAt > now()` ⚠️ (sau đó đã trả lời đúng bài `Seats`) |
| 17/10 | Bài 6: xong ✅ | Migration `AddConfigurations` đã apply; `\d "ShowtimeSeats"` có FK + unique; thử insert trùng tên phòng → lỗi `IX_Theaters_Name`, ghế vào phòng 999 → lỗi `FK_Seats_Theaters_TheaterId`. Commit `3294dce` | Lỡ `git commit -am` thành 2 commit (`-a` bỏ qua file mới) → gộp bằng `git reset --soft HEAD~2`. Thói quen: `git add .` → `git status` → commit |
| 17/10 | Bài 7: Testcontainers | Hiểu vì sao không dùng EF InMemory (không có unique/FK/lock) và không dùng chung DB dev; `async`/`await`, `IAsyncLifetime`; tự viết 2 assert (`GetPendingMigrationsAsync` rỗng, `ShowtimeSeats` có 0 dòng). Test xanh lần đầu (~25s) | |
| 17/10 | Bài 7: xong ✅ | Thử làm test đỏ cố ý rồi sửa lại; commit `8f79c03`; **push lần đầu lên GitHub**. Giai đoạn 0 xong sớm 1 ngày | |

## 🎤 Câu phỏng vấn đã luyện

- Vì sao tách `Seat` và `ShowtimeSeat`?
- Vì sao lưu `TotalPrice` thay vì tính lại từ `Showtime.Price`?
- Dependency rule trong Clean Architecture là gì?
- `ConfirmedAt` / `CancelledAt` dùng để làm gì? (doanh thu theo ngày lọc theo `ConfirmedAt`; chính sách hủy; tra soát khiếu nại)
- Vì sao dùng `decimal` cho tiền mà không dùng `double`? (double có lỗi làm tròn)
- Khóa ngoại (`ReservationId`) khác navigation property (`Reservation`) thế nào?
- Warning CS8618 nghĩa là gì, sửa thế nào? (property không-null nhưng chưa gán; `= string.Empty` hoặc `required`)
- Image, container, volume khác nhau thế nào? Vì sao không dùng `down -v` trên server?
- Vì sao không để mật khẩu trong code? (repo public ai cũng đọc được, bot quét GitHub; dùng biến môi trường)
- ORM / EF Core là gì? DbContext và migration là gì?
- Vì sao DbContext nằm ở Infrastructure chứ không ở Domain?
- Bảng `__EFMigrationsHistory` để làm gì? (ghi migration nào đã áp, để `update` chỉ chạy cái chưa áp)
- Dependency Injection là gì? (đăng ký 1 lần, framework tự tạo và đưa cho nơi cần)
- Vì sao không có cột `ExpiredAt`? (trùng với `ExpiresAt`; chỉ lưu thời điểm của sự kiện không đoán trước được)
- Fluent API configuration là gì? Vì sao không dùng attribute `[MaxLength]` trong Domain?
- Vì sao unique phải là cặp `(ShowtimeId, SeatId)` chứ không chỉ `SeatId`? (chỉ `SeatId` thì mỗi ghế chỉ được nằm trong 1 suất duy nhất)
- `virtual` / `override` / `base` khác nhau thế nào?
- ⚠️ (ôn 17/10: vẫn lẫn) Cascade, Restrict, SetNull khác nhau thế nào? Reservation dùng gì, SetNull nằm ở bảng nào? (OnDelete mô tả chuyện xảy ra với **con**; SetNull ở `ShowtimeSeat.ReservationId` vì cột đó cho phép null)
- Index là gì? Vì sao không đánh index lên mọi cột? (đọc nhanh nhưng ghi chậm, tốn chỗ)
- Unique index khác index thường thế nào? Khi nào unique không đủ? (luật khoảng thời gian, chồng lấn)
- ⚠️ (ôn 17/10: đúng lần 1, đúng thêm 1 lần nữa là ✅) Index `(A, B)` có giúp `WHERE B = ...` không? (không — leftmost prefix)
- ⚠️ (ôn 17/10: thiếu ý race condition) Vì sao code đã kiểm tra trùng rồi mà DB vẫn cần unique? (2 request cùng lúc đều kiểm tra "chưa có" → race condition)
- Integration test khác unit test thế nào? Vì sao dùng Testcontainers mà không dùng EF InMemory? (InMemory không có unique, FK, lock → test concurrency luôn pass dù code sai)
- Vì sao tên test nên dài, mô tả rõ? (test đỏ thì đọc tên là biết luật nào bị vỡ, không cần mở code)
