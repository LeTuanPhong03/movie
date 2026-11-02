namespace MovieReservation.Domain.Enums;

/// <summary>
/// Trạng thái một lần thanh toán. RefundRequired: VNPay báo đã trừ tiền nhưng đơn không xác nhận được
/// (đã hết hạn/đã huỷ đúng lúc đó) → phải hoàn tiền tay, không âm thầm nuốt tiền của khách.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    RefundRequired
}
