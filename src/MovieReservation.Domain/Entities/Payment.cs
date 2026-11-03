using MovieReservation.Domain.Enums;

namespace MovieReservation.Domain.Entities;

/// <summary>
/// Một lần thanh toán qua VNPay cho một đơn. Một đơn có thể có nhiều lần (khách bấm thanh toán lại),
/// mỗi lần một TxnRef riêng (mã gửi sang VNPay, VNPay trả lại trong IPN để tìm đúng dòng này).
/// Status chỉ đi một chiều: Pending → Succeeded/Failed/RefundRequired, Succeeded → RefundRequired (khách huỷ vé đã trả),
/// RefundRequired → Refunded (Admin đã hoàn). Mỗi bước là một UPDATE có điều kiện trạng thái cũ, nên IPN gọi lại
/// nhiều lần hay hai Admin bấm "đã hoàn" cùng lúc vẫn chỉ xử lý một lần (idempotent).
/// </summary>
public class Payment
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string TxnRef { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ProviderTransactionNo { get; set; }
    public string? ResponseCode { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? RefundedAt { get; set; }
}
