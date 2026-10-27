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

Test khẳng định chỉ 1 người được giữ ghế: trước khi sửa thì **đỏ** (Expected 1, Actual 6).

**Kết quả sau khi sửa** (26/10/2026, optimistic `xmin`, mục 4): 20 người đặt cùng 1 ghế thì **đúng 1 người** thành công, 19 người nhận `ConflictException`. Test **xanh**, toàn bộ 7/7 test xanh.

**Độ ổn định** (27/10/2026): chạy liên tiếp 10 lần → **10/10 xanh** (mỗi lần 13–17 giây). Test cũng kiểm tra cả 19 người thua đều nhận đúng `ConflictException`, không phải timeout hay lỗi khác.


## 4. Cách sửa

**Bối cảnh:** cần đóng khoảng hở giữa lúc kiểm tra ghế trống và lúc lưu (mục 2). Kiểm tra thêm trong C# không giải quyết được, vì C# chỉ thấy dữ liệu đã đọc từ trước. Phải để **DB** canh gác ngay lúc ghi.

### Các lựa chọn

| | ① Optimistic (lạc quan) | ② Pessimistic (bi quan) | ③ Atomic conditional update |
|---|---|---|---|
| **Cách hoạt động** | Mỗi dòng ghế có một version (`xmin` của Postgres). Lúc lưu, EF sinh `UPDATE ... WHERE Id=@id AND xmin=@version_lúc_đọc`. Người chậm hơn sửa được 0 dòng và EF ném `DbUpdateConcurrencyException`. | Mở transaction, đọc ghế bằng `SELECT ... FOR UPDATE` để khóa các dòng ghế. Người đến sau phải **chờ** tới khi người trước commit hoặc rollback. | Gộp kiểm tra và ghi vào **một** lệnh: `UPDATE ... SET Status='Held' WHERE Id IN (...) AND <ghế còn trống>`, rồi so số dòng đã sửa với số ghế yêu cầu. |
| **All-or-nothing** | Có sẵn: `SaveChanges` tự bọc trong một transaction | Phải tự mở transaction | Phải tự mở transaction và tự rollback khi thiếu dòng |
| **Ưu** | Sửa ít code nhất (giữ handler, thêm `try/catch`). Luật ghế trống vẫn chỉ nằm ở `IsAvailableAt`. Không ai phải chờ ai. | Dễ hiểu: làm lần lượt từng người. Người thua tự bị chặn bởi đoạn kiểm tra sẵn có. | Chỉ một lần gọi DB, nhanh nhất. Không cần cột version. |
| **Nhược** | Nói chung người thua phải retry, **nhưng ở bài này không cần**: thua tức là ghế đã mất, trả 409 là xong. | Request xếp hàng, mỗi request giữ một kết nối DB trong lúc chờ. **Deadlock** nếu hai người khóa ghế theo thứ tự ngược nhau (`[A1,A2]` và `[A2,A1]`), phải khóa theo `ORDER BY Id` để tránh. | Luật ghế trống bị **viết 2 nơi** (`IsAvailableAt` trong C# và điều kiện `WHERE` trong SQL). Sửa một chỗ quên chỗ kia là sinh bug. |

> Lưu ý: khóa `FOR UPDATE` chỉ kéo dài trong **một transaction** (vài mili giây), **không** phải 10 phút giữ ghế. Giữ ghế 10 phút là trạng thái `Held` + `HeldUntil` lưu trong bảng, không phải khóa của DB.

### Quyết định

Chọn **① Optimistic**, dùng cột `xmin` của Postgres làm version:
1. Ở bài toán đặt ghế, người thua **không cần retry**, nên nhược điểm lớn nhất của optimistic không còn.
2. Sửa ít nhất. All-or-nothing có sẵn nhờ `SaveChanges`. Luật ghế trống vẫn chỉ ở một chỗ.
3. Không có hàng chờ và không có deadlock như ②.
4. Dùng `xmin` thay vì cột `int` tự tăng: Postgres tự đổi `xmin` mỗi khi dòng bị sửa, nên không có chuyện quên `RowVersion++` ở một chỗ nào đó rồi mất bảo vệ mà không hay biết.

③ vẫn sẽ được dùng ở bước Confirm (30/10): `WHERE Status='Pending' AND ExpiresAt > now`.

### Hệ quả

- Người thua: EF ném `DbUpdateConcurrencyException`. **Handler** bắt lỗi này và ném `ConflictException`, rồi **middleware** đổi `ConflictException` thành HTTP 409. Người thua biết ngay là ghế đã có người lấy, không phải chờ.
- Cái giá: khi rất nhiều người tranh một ghế, ai cũng phải đọc rồi mới bị từ chối lúc lưu, tốn công hơn một chút so với ③. Với quy mô rạp phim thì chấp nhận được.
- Mọi chỗ sửa `ShowtimeSeat` sau này (Confirm, Cancel...) đều được `xmin` bảo vệ tự động.