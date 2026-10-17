# 📓 Nhật ký học — Movie Reservation

> File này cập nhật **sau mỗi buổi học**. Mở project ở phiên mới thì đọc mục **Hôm nay học gì** trước.
> Lộ trình tổng thể và các ngày đã tick nằm ở [PLAN.md](PLAN.md).

## 📍 Hôm nay học gì

**Cập nhật lần cuối:** 17/10/2026

**Bước tiếp theo:**
1. **Bài 3, gần xong:** build sạch 0 warning. Còn lại:
   - `Movie.Genre` đang là `int` (mất nghĩa). Chọn A: `string` + `= string.Empty`, hoặc B: enum `MovieGenre`, và giải thích được vì sao
   - **Commit** (entity + docs chưa commit)
2. Sau đó: commit → Docker Postgres → EF Core DbContext + migration

## 📅 Nhật ký

| Ngày | Bài | Đã học / đã làm | Còn yếu |
|---|---|---|---|
| 16/10 | Bài 1: ERD | 7 bảng và quan hệ; tách Seat/ShowtimeSeat; TotalPrice là snapshot; vòng đời ghế; viết `docs/decisions.md` | Hay nhầm tên trạng thái của **đơn** với của **ghế** |
| 16/10 | Dựng khung | Mentor tạo solution 6 project và thư mục; hiểu dependency rule (Api → Application → Domain); commit đầu tiên | |
| 17/10 | Bài 2: Enum | Viết `SeatStatus` và `ReservationStatus`; hiểu enum là gì; bài học "build không lỗi chưa chắc code đúng" | Lần đầu để `Expired` nhầm sang enum ghế (đã sửa); gõ sai tên `RevervationStatus` (đã sửa, commit `93e75be`) |
| 17/10 | Ôn ERD | Hiểu `ConfirmedAt`/`CancelledAt` lưu "lúc nào"; đơn Expired thì cả hai `null`; `ExpiresAt` đủ, không cần `ExpiredAt` | Trả lời đúng ngay lần đầu, nhầm lẫn trạng thái đã giảm |
| 17/10 | Bài 3: Entity | Tự viết đủ 7 entity; dùng `decimal` cho tiền, enum cho Status, `?` đúng chỗ. Học: khóa ngoại khác navigation property | Đặt tên chưa thống nhất (`ShowTime`/`Showtime`), lỗi chính tả, `Role` để string thay vì enum, `DurationMinutes` dùng `TimeSpan` |
| 17/10 | Bài 3: sửa review | Sửa 6/7 điểm; tự chọn tên `UserRole.Customer` (tốt hơn gợi ý); học warning CS8618 và cách `= string.Empty` | Sót `ShowTimeId` ở Reservation; dùng Ctrl+Shift+F để rà tên |

## 🎤 Câu phỏng vấn đã luyện

- Vì sao tách `Seat` và `ShowtimeSeat`?
- Vì sao lưu `TotalPrice` thay vì tính lại từ `Showtime.Price`?
- Dependency rule trong Clean Architecture là gì?
- `ConfirmedAt` / `CancelledAt` dùng để làm gì? (doanh thu theo ngày lọc theo `ConfirmedAt`; chính sách hủy; tra soát khiếu nại)
- Vì sao dùng `decimal` cho tiền mà không dùng `double`? (double có lỗi làm tròn)
- Khóa ngoại (`ReservationId`) khác navigation property (`Reservation`) thế nào?
- Warning CS8618 nghĩa là gì, sửa thế nào? (property không-null nhưng chưa gán; `= string.Empty` hoặc `required`)
- Vì sao không có cột `ExpiredAt`? (trùng với `ExpiresAt`; chỉ lưu thời điểm của sự kiện không đoán trước được)
