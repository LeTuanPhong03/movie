using MovieReservation.Domain.Enums;

namespace MovieReservation.Domain.Entities;

/// <summary>
/// Tài khoản người dùng. Role mặc định Customer: enum UserRole có Admin = 0 nên nếu không gán,
/// mọi chỗ quên gán Role (code mới, test, seed) sẽ âm thầm tạo ra Admin — mặc định phải là quyền thấp nhất.
/// IsOwner: tài khoản Admin tạo từ cấu hình (AdminSeeder) — không ai đổi được quyền, và chỉ Owner hạ được Admin khác,
/// để hệ thống không bao giờ hết Admin vì các Admin hạ quyền lẫn nhau.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public bool IsOwner { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
