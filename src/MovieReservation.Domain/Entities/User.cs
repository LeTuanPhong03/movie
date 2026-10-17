namespace MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;
public class User
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}