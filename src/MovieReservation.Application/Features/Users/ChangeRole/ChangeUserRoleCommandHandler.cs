using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;

namespace MovieReservation.Application.Features.Users.ChangeRole;

/// <summary>
/// Đổi Role của user bằng một câu UPDATE (0 dòng → 404). Không cho Admin tự đổi quyền mình: tự hạ về Customer
/// mà là Admin cuối cùng thì không còn ai quản trị được hệ thống.
/// Quyền mới có hiệu lực khi user lấy access token mới (đăng nhập lại hoặc refresh — refresh đọc Role từ DB),
/// tức chậm nhất sau AccessTokenMinutes; access token cũ vẫn mang Role cũ đến khi hết hạn.
/// </summary>
public class ChangeUserRoleCommandHandler(IAppDbContext db) : IRequestHandler<ChangeUserRoleCommand>
{
    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == request.CurrentUserId)
            throw new ConflictException("Không thể tự đổi quyền của chính mình.");

        int soDong = await db.Users
            .Where(u => u.Id == request.UserId)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, request.Role), cancellationToken);
        if (soDong == 0)
            throw new NotFoundException($"Không tìm thấy user {request.UserId}.");
    }
}
