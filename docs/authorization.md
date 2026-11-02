# Phân quyền endpoint

Ba mức: **Public** (không cần token), **User** (token hợp lệ, mọi role), **Admin** (policy `AdminOnly`).
Không token khi endpoint cần token → **401**. Có token nhưng sai role → **403**.

Dữ liệu cá nhân (đơn đặt vé) còn một lớp nữa: **kiểm tra chủ sở hữu** trong handler — `UserId` luôn lấy từ token, không bao giờ nhận từ body/query. Đơn của người khác trả **404** (không phải 403) để không lộ "đơn số này có tồn tại" khi kẻ xấu dò Id (chống IDOR).

| Method | Endpoint | Quyền | Ghi chú |
|---|---|---|---|
| POST | `/api/auth/register` | Public | luôn tạo role Customer |
| POST | `/api/auth/login` | Public | sai email/mật khẩu cùng một lỗi (chống dò email) |
| POST | `/api/auth/refresh` | Public | refresh token là bằng chứng; rotation |
| POST | `/api/auth/logout` | Public | xoá refresh token, idempotent (204) |
| GET | `/api/account/me` | User | |
| GET | `/api/account/admin-only` | Admin | endpoint thử policy |
| GET | `/api/movies`, `/api/movies/{id}` | Public | chỉ phim đang chiếu |
| POST / PUT / DELETE | `/api/movies[/{id}]` | Admin | xoá mềm mặc định |
| GET | `/api/theaters` | Admin | để chọn phòng khi tạo suất |
| GET | `/api/theaters/{id}` | User | |
| POST | `/api/theaters` | Admin | `Location` trỏ tới `GET /api/theaters/{id}` |
| GET | `/api/showtimes?date=`, `/api/showtimes/{id}`, `/api/showtimes/{id}/seats` | Public | |
| POST | `/api/showtimes` | Admin | chặn trùng giờ cùng phòng |
| GET | `/api/reservations` | User | chỉ đơn **của mình** (UserId từ token) |
| GET | `/api/reservations/{id}` | User + chủ đơn, hoặc Admin | người khác → 404 |
| POST | `/api/reservations` | User | |
| POST | `/api/reservations/{id}/payments` | User + chủ đơn | người khác → 404 |
| POST | `/api/reservations/{id}/confirm` | User + chủ đơn | **chỉ khi** `Reservations:AllowManualConfirm=true` (dev), không thì 404 |
| POST | `/api/reservations/{id}/cancel` | User + chủ đơn, hoặc Admin | người khác → 404; suất đã chiếu → 409 |
| GET | `/api/payments/vnpay/ipn` | Public + **chữ ký HMAC** | VNPay gọi; sai chữ ký → `97`, không đụng DB |
| GET | `/api/payments/vnpay/return` | Public + **chữ ký HMAC** | trang kết quả gọi; idempotent với IPN |
| GET | `/api/reports/revenue` | Admin | |
| GET | `/api/reports/capacity?from=&to=` | Admin | ghế bán / giữ / trống từng suất |
| GET | `/api/admin/reservations` | Admin | mọi đơn, lọc `showtimeId`, `userId`, `status` |
| GET | `/api/users?search=` | Admin | không trả PasswordHash |
| PUT | `/api/users/{id}/role` | Admin (+ luật Owner bên dưới) | có hiệu lực khi user lấy access token mới (≤ 15 phút) |

**Luật đổi quyền (Owner).** Owner = tài khoản Admin tạo từ cấu hình (`Admin:Email`, `User.IsOwner = true`, AdminSeeder tự đánh dấu cả với DB cũ).

| Ai đổi | Đổi ai | Kết quả |
|---|---|---|
| bất kỳ | chính mình | 409 |
| bất kỳ | Owner | 409 — không ai đổi được quyền Owner |
| Admin bất kỳ | Customer → Admin | 204 (đề bài: Admin nâng user lên Admin) |
| Admin thường | Admin → Customer | **403** — chỉ Owner hạ được Admin |
| Owner | Admin → Customer | 204 |

Vì sao: nếu Admin ngang quyền, Admin mới được nâng có thể hạ người đã nâng mình, hoặc hai Admin hạ nhau cùng lúc → hệ thống không còn Admin. Có Owner bất khả xâm phạm thì luôn còn ít nhất một Admin. Quyền của người gọi đọc từ DB, không tin token (token còn hạn có thể mang quyền cũ).

**Role mặc định là Customer** (`User.Role = UserRole.Customer`): enum có `Admin = 0`, nên nếu không gán thì chỗ nào quên gán Role sẽ âm thầm tạo ra Admin. Lỗi này được test `Tim_user_theo_email...` phát hiện ngày 02/11.

## Đã kiểm bằng test

- `ApiEndToEndTests`: báo cáo không token → 401, Customer → 403, Admin → 200; khách B gọi xem / huỷ / confirm / tạo thanh toán đơn của khách A → 404 cả bốn.
- `ReservationQueryTests`: xem đơn người khác và Id không tồn tại cùng ra 404; Admin xem được.
- `CancelReservationTests`: người khác huỷ → 404 và DB không đổi; Admin huỷ được.
- `VnPayPaymentTests`: IPN bị sửa tham số sau khi ký → 97, DB không đổi.
- `ApiEndToEndTests.Admin_nang_quyen_user...`: Customer gọi `/api/users`, `/api/admin/reservations`, `/api/reports/capacity` → 403; Admin nâng quyền → 204; token cũ vẫn 403, đăng nhập lại → 200; Admin tự hạ quyền mình → 409; Admin mới hạ Owner → 409, hạ Admin khác → 403, Owner hạ Admin → 204.
- `AdminFeatureTests`: cùng các luật Owner ở tầng handler, kiểm DB không đổi khi bị từ chối.
