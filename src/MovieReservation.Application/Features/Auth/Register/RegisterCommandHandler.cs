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
    AuthTokenIssuer tokenIssuer) : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // Chuẩn hóa email: " Phong@Gmail.com" và "phong@gmail.com" là cùng một hộp thư
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
        {
            throw new ConflictException("Email đã tồn tại.");
        }

        // Người đăng ký luôn là Customer, không cho client chọn Role
        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(request.Password),
            Role = UserRole.Customer
        };
        db.Users.Add(user);

        // Refresh token gắn vào user qua navigation → EF tự điền UserId.
        // Lưu một lần = một transaction: User và RefreshToken cùng thành công hoặc cùng thất bại.
        var refreshToken = tokenIssuer.AddRefreshToken(user);
        await db.SaveChangesAsync(cancellationToken);

        // Sau SaveChanges user.Id mới có giá trị → giờ mới sinh access token
        return tokenIssuer.CreateResponse(user, refreshToken);
    }
}
