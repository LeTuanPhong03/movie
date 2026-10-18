using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Auth.DTOs;
using MovieReservation.Domain.Entities;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Auth.Register;

/// <summary>
/// Đăng ký tài khoản Customer: kiểm tra email trùng, hash mật khẩu, lưu User + RefreshToken
/// trong một lần SaveChanges, rồi trả access token và refresh token.
/// </summary>
public class RegisterCommandHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    TimeProvider timeProvider) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Chuẩn hóa email: " Phong@Gmail.com" và "phong@gmail.com" là cùng một hộp thư
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        // 2. Email đã tồn tại
        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("Email đã tồn tại.");
        }

        // 3. Tạo User (người đăng ký luôn là Customer)
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            Role = UserRole.Customer
        };

        // 4. Refresh token gắn vào user qua navigation → EF tự điền UserId khi lưu
        var refreshTokenResult = tokenService.GenerateRefreshToken();
        var refreshToken = new RefreshToken
        {
            User = user,
            TokenHash = refreshTokenResult.RefreshTokenHash,
            ExpiresAt = refreshTokenResult.ExpiresAt,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };

        // 5. Lưu một lần = một transaction: User và RefreshToken cùng thành công hoặc cùng thất bại
        db.Users.Add(user);
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(cancellationToken);

        // 6. Sau SaveChanges user.Id mới có giá trị → giờ mới sinh access token
        var accessTokenResult = tokenService.GenerateAccessToken(user);

        // 7. Trả kết quả
        return new AuthResponse(
            User: new UserDto(user.Id, user.Email, user.FullName, user.Role.ToString()),
            AccessToken: accessTokenResult.AccessToken,
            RefreshToken: refreshTokenResult.RefreshToken,
            AccessTokenExpiresAt: accessTokenResult.ExpiresAt);
    }
}
