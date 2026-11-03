namespace MovieReservation.Domain.Enums;

/// <summary>
/// Trạng thái một lần thanh toán. RefundRequired: đã thu tiền nhưng khách không còn vé — VNPay trừ tiền khi đơn
/// đã hết hạn/huỷ, hoặc khách huỷ vé đã thanh toán → phải hoàn tiền, không âm thầm nuốt tiền của khách.
/// Refunded: Admin đã hoàn (tay, ngoài hệ thống) và đánh dấu lại.
/// </summary>
public enum PaymentStatus
{
    Pending,
    Succeeded,
    Failed,
    RefundRequired,
    Refunded
}
