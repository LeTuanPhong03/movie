# lý do tách Seat và ShowtimeSeat, lý do lưu TotalPrice
1. Seat là ghế cứng của phòng. Nếu lưu trạng thái vào Seat, đặt ghế A5 suất 18:00 sẽ làm A5 bị khóa ở mọi suất khác. Vì vậy trạng thái được lưu ở ShowtimeSeat, mỗi dòng là một ghế trong một suất.
2. TotalPrice giống như 1 snapShot để sau này tham chiếu lại giá mà không ảnh hưởng price khi admin đổi giá vé và báo cáo doanh thu vẫn đúng với số tiền khách đã trả thật.

# 3. Truy cập DB từ Application: chọn `IAppDbContext` thay vì Repository (18/10)

**Bối cảnh:** handler ở Application cần đọc/ghi DB, nhưng `AppDbContext` nằm ở Infrastructure và Application không được tham chiếu Infrastructure. Có 2 cách:
- **Cách A — Repository** (đã dùng ở project E-Commerce): mỗi bảng một interface (`IUserRepository`...) và một class cài đặt.
- **Cách B — `IAppDbContext`**: một interface chứa các `DbSet` và `SaveChangesAsync`, `AppDbContext` cài đặt nó; handler viết LINQ trực tiếp.

| | Cách A: Repository | Cách B: IAppDbContext |
|---|---|---|
| Ưu điểm | Application sạch hoàn toàn, không biết EF Core; đổi ORM/DB chỉ sửa Infrastructure; unit test handler dễ (mock repository) | Ít code (một interface cho cả project); transaction nhiều bảng tự nhiên (một lần `SaveChangesAsync`); dùng được hết tính năng EF (`ExecuteUpdate`, `AsNoTracking`, projection) |
| Nhược điểm | Nhiều file, mỗi truy vấn mới phải thêm method; transaction nhiều bảng cần thêm Unit of Work; tính năng mạnh của EF bị giấu sau repository | Application phụ thuộc thư viện EF Core; đổi ORM phải sửa handler; unit test handler khó (mock `DbSet` phiền) |

**Quyết định:** chọn **Cách B**.

**Lý do:** trọng tâm của project là chống double-booking. Đặt vé phải cập nhật `Reservation` và nhiều `ShowtimeSeat` trong **cùng một transaction**, và cần câu `UPDATE ... WHERE Status = 'Available'` rồi kiểm tra số dòng bị ảnh hưởng. Cách B làm việc này trực tiếp; cách A phải thêm Unit of Work và bọc từng truy vấn đặc biệt thành method.

**Đánh đổi chấp nhận:**
- Application phụ thuộc thư viện EF Core (không phụ thuộc project Infrastructure — dependency rule vẫn giữ). EF Core bản thân đã là repository + unit of work, nên không bọc thêm một lớp nữa.
- Khó unit test handler → test handler bằng **integration test với Postgres thật (Testcontainers)**, vốn là cách project đã chọn để test concurrency.
- Đổi ORM khó hơn → project không có kế hoạch đổi ORM.
# 4. Ghế VIP: để sau, v1 mọi ghế cùng giá (20/10)

**Bối cảnh:** rạp thật có ghế thường và ghế VIP với giá khác nhau. Người học đề xuất khi thiết kế `CreateTheaterCommand`.

**Quyết định:** v1 **mọi ghế trong một suất chiếu cùng giá** (`Showtime.Price`). Ghi vào mục "Tính năng tương lai" trong `PLAN.md`.

**Lý do:** yêu cầu gốc (roadmap.sh) không đòi; thêm loại ghế kéo theo sửa `Seat`, input tạo phòng, cách tính giá và ERD (~nửa ngày) trong khi deadline 15/11 và trọng tâm là chống double-booking.

**Thiết kế hiện tại vẫn mở rộng được:** thêm cột `SeatType` vào `Seat` bằng một migration (`defaultValue` = Standard cho ghế cũ); `TotalPrice` đã là snapshot nên đơn cũ không bị ảnh hưởng khi đổi cách tính giá.

# 5. Movie: xóa mềm, chặn sửa thời lượng, phân trang báo lỗi (20/10)

**Xóa phim → soft delete (`IsActive = false`)** thay vì xóa thật: `Showtime → Movie` là `Restrict`, phim đã có suất/vé cần giữ lịch sử; bật lại dễ, không phải tạo lại. Hệ quả: truy vấn cho khách phải lọc `IsActive`; phim ẩn không tạo suất mới được.

**Hard delete có điều kiện (`DELETE /api/movies/{id}?hardDelete=true`)** — người học đề xuất: dọn phim tạo nhầm. Chỉ cho phép khi phim **chưa từng có suất chiếu** (kể cả suất đã chiếu xong), nếu có → 409 "chỉ có thể ẩn". Mặc định (không gửi query) là soft — bên an toàn là bên xảy ra khi client không nói gì. Kiểm tra idempotent ("đã ẩn rồi → bỏ qua") chỉ nằm trong nhánh soft, để hard delete phim đã ẩn vẫn chạy.

**Sửa `DurationMinutes` → chặn (409) nếu phim còn suất chưa kết thúc (`EndAt > now`).** `Showtime.EndAt` là cột đã lưu, không tự cập nhật; để nguyên thì thực tế chồng phòng mà hệ thống không biết, tính lại thì có thể vi phạm luật không chồng giờ. Dùng `EndAt` (không phải `StartAt`) để tính cả suất đang chiếu dở; suất đã chiếu xong không ảnh hưởng nên không chặn.

**Phân trang: `pageSize` sai → 400**, không âm thầm cắt về max (client không biết mình nhận ít hơn yêu cầu). Không gửi tham số thì dùng mặc định `page = 1`, `pageSize = 10`; `MaxPageSize = 50`.

# 6. Bảng `ReservationSeats`: bản chụp ghế của đơn (02/11)

**Vấn đề:** khi huỷ/hết hạn, ghế được nhả bằng cách đặt `ShowtimeSeat.ReservationId = null`. Nếu lịch sử đơn chỉ dựa vào `ShowtimeSeat` thì đơn Cancelled/Expired **mất luôn danh sách ghế** ("tôi đã huỷ đơn ghế nào?" không trả lời được), và báo cáo không đếm được số vé.

**Quyết định:** thêm bảng `ReservationSeats(ReservationId, SeatId)`, ghi **một lần** lúc đặt, không bao giờ sửa — cùng ý tưởng với `TotalPrice` (mục 1). `ShowtimeSeat` trả lời "ghế này **bây giờ** của ai", `ReservationSeat` trả lời "đơn này **đã từng** đặt ghế nào".

**Đánh đổi:** thêm một bảng và một lần INSERT mỗi ghế khi đặt (cùng một `SaveChanges`, không thêm round-trip). Đơn tạo trước migration chỉ khôi phục được ghế của đơn còn giữ/đã bán; đơn đã huỷ trước đó mất ghế vĩnh viễn (dữ liệu dev, chấp nhận).

# 7. Thanh toán VNPay (02/11)

**Luồng:** `POST /api/reservations/{id}/payments` tạo một dòng `Payment` (Pending, `TxnRef` = Guid) và link VNPay có chữ ký → khách trả tiền trên VNPay → VNPay gọi **IPN** (server ↔ server) và chuyển trình duyệt về **return URL**. Cả hai cùng gọi `ProcessVnPayCallbackCommand`.

**Thứ tự kiểm tra trong callback:** chữ ký HMAC-SHA512 → `TxnRef` có thật → số tiền khớp `Payment.Amount` → Payment còn Pending. Chữ ký kiểm **đầu tiên** vì URL callback là public, ai cũng gọi được; chưa qua chữ ký thì không tin bất kỳ tham số nào.

**Idempotent:** `UPDATE "Payments" SET Status = ... WHERE Id = @id AND Status = 'Pending'` rồi đếm số dòng — cùng kỹ thuật với Confirm/Cancel. VNPay gọi IPN nhiều lần, hoặc IPN và return URL tới cùng lúc → chỉ một bên sửa được 1 dòng, bên kia nhận "02 đã xử lý". Có test 10 IPN song song.

**Trả tiền xong nhưng đơn đã hết hạn — chọn: KHÔNG kéo dài hạn giữ ghế.**
- `vnp_ExpireDate` = `Reservation.ExpiresAt` → VNPay tự chặn thanh toán sau hạn giữ ghế, phần lớn trường hợp không xảy ra.
- Kéo dài hạn khi bắt đầu thanh toán thì bị lạm dụng: bấm "thanh toán" liên tục để giữ ghế mãi mà không trả tiền.
- Ca còn sót (IPN tới trễ, hoặc đơn bị huỷ đúng lúc): Payment chuyển `RefundRequired`, ghi log cảnh báo, vẫn trả VNPay "00" (đã ghi nhận, không cần gọi lại). Hoàn tiền làm tay — **Known limitation**.

**Return URL cũng xử lý (không chỉ hiển thị):** chạy local không có URL public thì IPN không bao giờ tới; vì lệnh idempotent nên để return URL xử lý luôn là an toàn. Trang kết quả **không** tự đọc `vnp_ResponseCode` trên URL (ai cũng sửa được URL) mà hỏi API.

**Nút confirm giả** (`POST /confirm`) chỉ bật khi `Reservations:AllowManualConfirm = true` (Development). Có thanh toán thật mà vẫn mở nút này ở production thì ai cũng xác nhận được vé không cần trả tiền.

# 8. Báo cáo doanh thu (02/11)

- **Chỉ tính đơn đang `Confirmed`.** Đơn Confirmed rồi bị huỷ có `Status = Cancelled` → tự rơi khỏi báo cáo (coi như hoàn tiền). Pending/Expired chưa thu đồng nào.
- **Tiền = `TotalPrice`** (giá chụp lúc đặt), không nhân lại `Showtime.Price` hiện tại.
- **Ngày = ngày xác nhận (`ConfirmedAt`) theo giờ Việt Nam**, tức ngày thu tiền — không phải ngày chiếu. Lọc bằng khoảng UTC `[from, to)`; nhóm theo ngày bằng `ConfirmedAt + 7 giờ` (VN không đổi giờ mùa hè nên +7 luôn đúng). Test có ca 23:59 VN và 00:00 VN rơi vào 2 ngày khác nhau.
- `GROUP BY` chạy trong SQL, không tải từng đơn về C#. Có test đối chiếu tổng với câu SQL viết tay.
- Khoảng tối đa 366 ngày, `From > To` → 400.

# 9. Huỷ vé đã thanh toán và hoàn tiền (03/11)

**Lỗi phát hiện khi rà luồng thực tế:** thêm VNPay (02/11) làm "huỷ vé" đổi nghĩa — huỷ vé đã trả tiền nghĩa là nợ khách tiền — nhưng luồng Huỷ không được mở lại. Đơn Confirmed huỷ xong thì `Payment` vẫn `Succeeded`, không ai biết phải hoàn.

- **Cùng một transaction với lệnh huỷ:** `UPDATE Payments SET Status='RefundRequired' WHERE ReservationId=@id AND Status='Succeeded'`. Huỷ thành công thì chắc chắn có dòng "cần hoàn"; huỷ thất bại thì không có gì đổi.
- **Huỷ đúng lúc IPN tới** vẫn ra RefundRequired theo cả hai thứ tự: huỷ trước → IPN thấy đơn Cancelled, `TryConfirm` sửa 0 dòng → RefundRequired (đã có từ mục 7); IPN trước → đơn Confirmed + Payment Succeeded → lệnh huỷ đổi sang RefundRequired. Hai bên cùng UPDATE dòng Reservation nên DB xếp hàng, không có thứ tự thứ ba.
- **Hạn chót huỷ vé đã trả:** khách chỉ tự huỷ khi còn hơn `Reservation.PaidCancelCutoffMinutes` (120 phút) tới giờ chiếu — rạp còn kịp bán lại ghế. Điều kiện nằm trong `WHERE` của câu huỷ (không đọc trước rồi kiểm). Đơn Pending (chưa trả) huỷ được tới giờ chiếu; **Admin** không bị giới hạn (rạp chủ động huỷ). Bị chặn → 409 nói rõ lý do.
- **Hoàn tiền làm tay**, hệ thống chỉ ghi nhận: `RefundRequired → Refunded` (+ `RefundedAt`) bằng UPDATE có điều kiện trạng thái cũ → hai Admin bấm cùng lúc chỉ một lần được ghi nhận, người sau 409. Chưa gọi API hoàn tiền của VNPay.
- Báo cáo doanh thu không đổi: đơn Cancelled vốn đã không được tính.

# 10. Lưu token ở trình duyệt: access token trong RAM, refresh token trong cookie HttpOnly (03/11)

**Trước:** FE lưu cả access lẫn refresh token trong `localStorage`. Trang dính XSS (một script độc chạy được trên domain FE) đọc được `localStorage` → lấy refresh token mang đi dùng ở máy khác suốt 7 ngày.

**Chọn:**
- **Refresh token → cookie** `movie_refresh`: `HttpOnly` (JS không đọc được), `Secure`, `SameSite=Lax` (trang web khác gửi POST sang thì trình duyệt không kèm cookie → chặn CSRF), `Path=/api/auth` (chỉ đi theo refresh/logout). Body JSON (`SessionResponse`) không còn refresh token. `/refresh` và `/logout` đọc token từ cookie, không nhận body.
- **Access token → biến JS trong RAM.** F5 là mất → FE gọi `/refresh` lúc mở trang để lấy lại. Access token sống 15' nên lỡ bị XSS lấy được cũng hết hạn nhanh.
- Handler không đổi: vẫn nhận chuỗi token. Đọc/ghi cookie là việc của HTTP nên nằm ở `AuthController` (`Api/Auth/RefreshTokenCookie`).

**FE gọi BE qua proxy cùng domain** (Next `rewrites`: `/api/*` → BE). FE (Vercel) và BE (Render) khác site → cookie BE đặt là **cookie bên thứ ba**, Safari đã chặn và Chrome đang chặn dần; muốn gửi được còn phải `SameSite=None` (mất lớp chặn CSRF). Qua proxy thì trình duyệt chỉ thấy một domain → cookie first-party, `SameSite=Lax` dùng được, khỏi cần CORS cho FE.

**Nhiều tab:** các tab dùng chung một cookie, mà rotation cho mỗi refresh token dùng đúng một lần → 2 tab F5 cùng lúc gửi cùng token, tab chậm hơn bị 401 và đăng xuất oan. FE dùng **Web Locks API** (`navigator.locks`) để các tab xếp hàng gọi `/refresh`; tab sau gửi khi trình duyệt đã ghi cookie mới.

**Không chọn:**
- *Giữ localStorage:* đơn giản nhất nhưng XSS lấy được phiên dài hạn.
- *Cả access token cũng vào cookie (BE đọc cookie thay header Bearer):* mọi API đều chạy bằng cookie → phải chống CSRF cho mọi endpoint ghi (anti-forgery token), Swagger / client khác khó dùng.
- *BFF (Backend-for-Frontend):* server Next giữ hết token, trình duyệt chỉ có session cookie — an toàn nhất (khuyến nghị của IETF cho ứng dụng trình duyệt) nhưng phải viết thêm tầng server lưu session; quá sức so với dự án.

**Còn lại:** HttpOnly không chống được XSS *dùng* phiên ngay trên trang (script gọi API thay người dùng khi tab đang mở) — chỉ chống *mang token đi*. Chống XSS thật vẫn là không chèn HTML chưa lọc + Content-Security-Policy.
