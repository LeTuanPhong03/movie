using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;

namespace MovieReservation.Application.Features.Auth.Login;
/// <summary>
/// Handler cho LoginCommand, sử dụng MediatR.
/// </summary>

public class LoginCommandHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider
) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Email hoặc Password không hợp lệ.");
        }
        
        var accessTokenResult = tokenService.GenerateAccessToken(user);
        var refreshTokenResult = tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            User = user,
            TokenHash = refreshTokenResult.RefreshTokenHash,
            ExpiresAt = refreshTokenResult.ExpiresAt,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);
        return new AuthResponse(
            RefreshToken: refreshTokenResult.RefreshToken,
            User: new UserDto(
                Id: user.Id,
                FullName: user.FullName,
                Email: user.Email,
                Role: user.Role.ToString()),
            AccessTokenExpiresAt: accessTokenResult.ExpiresAt,
            AccessToken: accessTokenResult.AccessToken);
    }
}