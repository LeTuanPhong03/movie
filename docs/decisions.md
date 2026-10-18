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