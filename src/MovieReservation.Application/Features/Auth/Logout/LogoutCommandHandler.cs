using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;

namespace MovieReservation.Application.Features.Auth.Logout;

/// <summary>
/// Đăng xuất: xóa dòng refresh token tương ứng. Idempotent — token không tồn tại (đã logout,
/// đã dùng, token bịa) vẫn coi là thành công. Thiết bị khác giữ token riêng nên không bị ảnh hưởng.
/// Access token cũ vẫn sống tới khi hết hạn (tối đa vài phút) — client tự xóa nó.
/// </summary>
public class LogoutCommandHandler(IAppDbContext db, ITokenService tokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashToken(request.RefreshToken);

        // ExecuteDeleteAsync: chạy thẳng "DELETE ... WHERE TokenHash = ..." trong 1 câu SQL,
        // không cần đọc lên rồi Remove + SaveChanges. Xóa 0 dòng cũng không lỗi → tự nhiên idempotent.
        await db.RefreshTokens
            .Where(t => t.TokenHash == tokenHash)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
