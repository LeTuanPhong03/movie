using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Auth.DTOs;

namespace MovieReservation.Application.Features.Auth.Refresh;

/// <summary>
/// Làm mới token theo kiểu rotation: tìm refresh token theo hash, còn hạn thì xóa token cũ
/// và cấp token mới trong cùng một SaveChanges. Mỗi refresh token chỉ dùng được một lần.
/// </summary>
public class RefreshTokenCommandHandler(
    IAppDbContext db,
    ITokenService tokenService,
    AuthTokenIssuer tokenIssuer,
    TimeProvider timeProvider) : IRequestHandler<RefreshTokenCommand, AuthResponse>
{
    private const string InvalidTokenMessage = "Refresh token không hợp lệ hoặc đã hết hạn.";

    public async Task<AuthResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // DB chỉ lưu hash → hash token client gửi lên rồi tìm đúng dòng
        var tokenHash = tokenService.HashToken(request.RefreshToken);
        var storedToken = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

        // Không thấy = token bịa, đã dùng rồi, hoặc đã đăng xuất
        if (storedToken is null || storedToken.ExpiresAt <= timeProvider.GetUtcNow().UtcDateTime)
        {
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        // Rotation: xóa cũ + thêm mới trong cùng một transaction
        var user = storedToken.User;
        db.RefreshTokens.Remove(storedToken);
        var newRefreshToken = tokenIssuer.AddRefreshToken(user);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // 2 request cùng dùng 1 token: bên lưu sau xóa một dòng đã không còn → EF ném lỗi này.
            // Chỉ một bên được token mới, bên còn lại bị từ chối.
            throw new UnauthorizedException(InvalidTokenMessage);
        }

        return tokenIssuer.CreateResponse(user, newRefreshToken);
    }
}
