# 📓 Nhật ký học — Movie Reservation

> File này cập nhật **sau mỗi buổi học**. Mở project ở phiên mới thì đọc mục **Hôm nay học gì** trước.
> Lộ trình tổng thể và các ngày đã tick nằm ở [PLAN.md](PLAN.md).

## 📍 Hôm nay học gì

**Cập nhật lần cuối:** 17/10/2026

**Bước tiếp theo:**
1. **Bài 6:** Configurations (Fluent API) trong `Infrastructure/Persistence/Configurations/` để sửa 3 vấn đề thấy qua `\d "ShowtimeSeats"`:
   - chưa có **khóa ngoại** (entity chỉ có `int SeatId`, chưa có navigation property)
   - chưa có **unique `(ShowtimeId, SeatId)`**, nên DB cho 2 dòng cùng ghế cùng suất
   - `Status` lưu dạng **số**, nên đổi sang lưu chữ
   - thêm giới hạn độ dài chuỗi, unique `Email` → migration thứ 2
2. Sau đó: test đầu tiên với Testcontainers
3. **Cuối mỗi buổi:** ôn 2–3 câu trong kho phỏng vấn (xem cách ôn bên dưới)

## 🔁 Cách ôn câu phỏng vấn

- Kho câu hỏi ở cuối file **không phải để trả lời hết ngay**, mà dùng để ôn dần.
- Cuối mỗi buổi, mentor hỏi **2–3 câu ngẫu nhiên** (ưu tiên câu cũ). Trả lời bằng lời của mình, không nhìn tài liệu.
- Câu trả lời sai hoặc ấp úng thì đánh dấu ⚠️ cạnh câu đó để hỏi lại hôm sau. Trả lời tốt 2 lần liền thì đánh dấu ✅.
- Gợi ý trong ngoặc chỉ để tự kiểm tra, **không học thuộc**.
- Chưa ôn buổi nào (tính đến 17/10).

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
