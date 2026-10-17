namespace MovieReservation.Application.Features.Auth.DTOs;

public record AuthResponse(
    string UserId,
    string Email,
    string FullName,
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt
); 