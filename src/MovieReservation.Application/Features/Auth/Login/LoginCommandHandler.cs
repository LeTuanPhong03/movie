using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Auth.Login;

/// <summary>
/// Đăng nhập: chuẩn hóa email giống Register, kiểm tra mật khẩu (sai email hay sai mật khẩu
/// đều báo chung một câu để chống dò tài khoản), lưu refresh token mới, trả cặp token.
/// </summary>
public class LoginCommandHandler(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    AuthTokenIssuer tokenIssuer) : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");
        }

        // Mỗi lần login thêm 1 refresh token → đăng nhập nhiều thiết bị cùng lúc được
        var refreshToken = tokenIssuer.AddRefreshToken(user);
        await db.SaveChangesAsync(cancellationToken);

        return tokenIssuer.CreateResponse(user, refreshToken);
    }
}
