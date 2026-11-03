using MediatR;

namespace MovieReservation.Application.Features.Refunds.MarkRefunded;

/// <summary>
/// Admin xác nhận đã hoàn tiền (tay, trên cổng VNPay hoặc chuyển khoản) cho một Payment đang RefundRequired.
/// </summary>
public record MarkRefundedCommand(int PaymentId) : IRequest;
