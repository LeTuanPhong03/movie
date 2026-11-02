using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Users.ChangeRole;

/// <summary>
/// Đổi Role của user theo 3 luật để hệ thống không bao giờ hết Admin:
/// (1) không tự đổi quyền mình (409); (2) không ai đổi được quyền của Owner (409);
/// (3) Admin nào cũng nâng được user lên Admin, nhưng chỉ Owner hạ được Admin khác (403).
/// Quyền của người gọi đọc từ DB chứ không tin token: token còn hạn có thể mang Role/Owner cũ.
/// Câu UPDATE giữ điều kiện !IsOwner trong WHERE như lớp phòng thủ cuối.
/// Quyền mới có hiệu lực khi user lấy access token mới (đăng nhập lại / refresh), chậm nhất AccessTokenMinutes.
/// </summary>
public class ChangeUserRoleCommandHandler(IAppDbContext db) : IRequestHandler<ChangeUserRoleCommand>
{
    public async Task Handle(ChangeUserRoleCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == request.CurrentUserId)
            throw new ConflictException("Không thể tự đổi quyền của chính mình.");

        var target = await db.Users.AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new { u.Role, u.IsOwner })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy user {request.UserId}.");
        if (target.IsOwner)
            throw new ConflictException("Không thể đổi quyền của Owner.");

        bool haAdmin = target.Role == UserRole.Admin && request.Role != UserRole.Admin;
        if (haAdmin)
        {
            bool nguoiGoiLaOwner = await db.Users.AnyAsync(u => u.Id == request.CurrentUserId && u.IsOwner, cancellationToken);
            if (!nguoiGoiLaOwner)
                throw new ForbiddenException("Chỉ Owner mới hạ được quyền của Admin khác.");
        }

        await db.Users
            .Where(u => u.Id == request.UserId && !u.IsOwner)
            .ExecuteUpdateAsync(s => s.SetProperty(u => u.Role, request.Role), cancellationToken);
    }
}
