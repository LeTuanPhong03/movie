using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Refunds.MarkRefunded;

/// <summary>
/// RefundRequired → Refunded bằng một UPDATE có điều kiện trạng thái cũ: hai Admin bấm cùng lúc thì chỉ một người
/// sửa được 1 dòng, người kia nhận 0 dòng → 409 (đã hoàn rồi), tránh ghi nhận hoàn tiền hai lần. Không có Payment → 404.
/// </summary>
public class MarkRefundedCommandHandler(IAppDbContext db, TimeProvider timeProvider) : IRequestHandler<MarkRefundedCommand>
{
    public async Task Handle(MarkRefundedCommand request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        int soDong = await db.Payments
            .Where(p => p.Id == request.PaymentId && p.Status == PaymentStatus.RefundRequired)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, PaymentStatus.Refunded)
                .SetProperty(p => p.RefundedAt, now), cancellationToken);
        if (soDong > 0)
            return;

        if (!await db.Payments.AnyAsync(p => p.Id == request.PaymentId, cancellationToken))
            throw new NotFoundException($"Không tìm thấy thanh toán {request.PaymentId}.");
        throw new ConflictException("Khoản này không ở trạng thái chờ hoàn tiền (có thể đã được đánh dấu hoàn rồi).");
    }
}
