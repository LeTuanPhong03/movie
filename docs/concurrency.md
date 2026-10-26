# Concurrency: chống đặt trùng ghế (double-booking)

## 1. Vấn đề

Khi nhiều khách cùng đặt **một ghế** trong cùng một lúc, nhiều người có thể cùng đặt thành công.
Mỗi người đều nhận phản hồi thành công và có một đơn riêng trong DB, nhưng dòng ghế chỉ trỏ về người lưu cuối cùng.
Không có lỗi nào được báo ra, nên không ai biết: các khách đều thanh toán được, và đến rạp thì nhiều người cùng một ghế.

## 2. Vì sao xảy ra

Handler đặt vé làm 2 việc tách rời: **đọc** ghế để kiểm tra còn trống, rồi **lưu** đơn + đổi ghế sang Held.
Lỗi xảy ra khi 2 request **xen kẽ** nhau:

| Thời điểm | Khách A | Khách B |
|---|---|---|
| t1 | Đọc ghế A1 → trống | |
| t2 | | Đọc ghế A1 → **vẫn trống** (A chưa lưu) |
| t3 | Lưu đơn, ghế A1 → Held → thành công | |
| t4 | | Lưu đơn, ghế A1 → Held → **cũng thành công** |

Vì sao bước kiểm tra ghế trống không ngăn được: B đã **qua bước kiểm tra ở t2**, lúc ghế còn trống, nên tới t4 chỉ việc lưu. Lệnh lưu không hỏi lại ghế còn trống không, trong khi kết quả kiểm tra của B **đã cũ** (A giữ ghế ở t3). Lỗi nằm ở **khoảng hở giữa lúc kiểm tra và lúc lưu** (lỗi kiểu *check-then-act*).

Kết quả trong DB: bảng `Reservations` có **2 đơn** Pending cho cùng ghế A1; dòng ghế A1 chỉ còn `ReservationId` của **B** (người lưu sau ghi đè người lưu trước — *last write wins*).

## 3. Bằng chứng

**Test:** `tests/MovieReservation.IntegrationTests/DoubleBookingTests.cs`, test `Hai_muoi_nguoi_dat_cung_1_ghe_thi_chi_1_nguoi_thanh_cong`

- Postgres thật (Testcontainers), seed 1 suất chiếu có ghế.
- 20 task chạy song song, cùng đặt **1 ghế**. Mỗi task dùng **DbContext riêng**, giống 20 request thật (ASP.NET tạo DbContext theo từng request).
- Gọi thẳng `CreateReservationCommandHandler`, đếm số task đặt thành công và số đơn trong DB.

**Chạy:**

```powershell
dotnet test tests/MovieReservation.IntegrationTests --filter DoubleBooking
```

**Kết quả trước khi sửa** (26/10/2026, 3 lần chạy liên tiếp):

| Lần chạy | Số người cùng giữ được 1 ghế |
|---|---|
| 1 | 10/20 |
| 2 | 20/20 |
| 3 | 12/20 |

Nhận xét: con số thay đổi giữa các lần vì nó phụ thuộc vào **thời điểm** các request tình cờ chạy chồng lên nhau, mỗi lần chạy lệch nhau một chút. Điều đó cho thấy test bằng tay (bấm đặt thử 1–2 lần) gần như **không thể phát hiện** lỗi này: hai lần bấm của một người cách nhau cả giây, còn khoảng hở chỉ vài mili giây. Cần test chạy song song có chủ đích như trên.

Test khẳng định chỉ 1 người được giữ ghế, nên hiện đang **đỏ**; sẽ **xanh** khi lỗi được sửa (mục 4).

## 4. Cách sửa

_(27/10: so sánh 3 hướng và chọn một.)_
