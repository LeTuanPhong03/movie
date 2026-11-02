namespace MovieReservation.Domain.Entities;

/// <summary>
/// Bản chụp "đơn này đã đặt ghế nào", ghi một lần lúc đặt và không bao giờ sửa (giống TotalPrice).
/// Cần riêng bảng này vì ShowtimeSeat.ReservationId bị xoá về null khi huỷ/hết hạn để nhả ghế
/// → nếu chỉ dựa vào ShowtimeSeat thì lịch sử đơn Cancelled/Expired mất sạch danh sách ghế.
/// </summary>
public class ReservationSeat
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public int SeatId { get; set; }
    public Seat Seat { get; set; } = null!;
}
