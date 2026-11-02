using MovieReservation.Domain.Enums;

namespace MovieReservation.Domain.Entities;

/// <summary>
/// Tài khoản người dùng. Role mặc định Customer: enum UserRole có Admin = 0 nên nếu không gán,
/// mọi chỗ quên gán Role (code mới, test, seed) sẽ âm thầm tạo ra Admin — mặc định phải là quyền thấp nhất.
/// </summary>
public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Customer;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
