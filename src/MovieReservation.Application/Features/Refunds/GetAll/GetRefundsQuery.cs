using MediatR;
using MovieReservation.Application.Common.Models;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Refunds.GetAll;

/// <summary>
/// Admin xem các khoản phải hoàn (mặc định RefundRequired) hoặc đã hoàn (Refunded), mới nhất trước.
/// Mỗi dòng đủ thông tin để hoàn tay trên cổng VNPay: TxnRef, mã giao dịch VNPay, số tiền, người đặt.
/// </summary>
public record GetRefundsQuery(PaymentStatus Status = PaymentStatus.RefundRequired, int Page = 1, int PageSize = 20)
    : IRequest<PagedResult<RefundDto>>
{
    public const int MaxPageSize = 100;
}

/// <param name="ReservationStatus">Lý do phải hoàn: Cancelled = khách/Admin huỷ vé đã trả; Expired/Pending = VNPay trừ tiền khi đơn đã hết hạn.</param>
public record RefundDto(
    int PaymentId,
    int ReservationId,
    ReservationStatus ReservationStatus,
    string UserEmail,
    string UserFullName,
    string MovieTitle,
    DateTime StartAt,
    decimal Amount,
    string TxnRef,
    string? ProviderTransactionNo,
    PaymentStatus Status,
    DateTime? PaidAt,
    DateTime? RefundedAt);
