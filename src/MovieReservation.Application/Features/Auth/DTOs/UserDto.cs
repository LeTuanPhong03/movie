namespace MovieReservation.Application.Features.Auth.DTOs;

/// <summary>
/// Thông tin người dùng trả cho frontend (không bao giờ chứa PasswordHash).
/// Role ở dạng chữ ("Admin" / "Customer") để frontend ẩn/hiện chức năng.
/// </summary>
public record UserDto(int Id, string Email, string FullName, string Role);
