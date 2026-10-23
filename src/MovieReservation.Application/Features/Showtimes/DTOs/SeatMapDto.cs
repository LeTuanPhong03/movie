using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Showtimes.DTOs;

/// <summary>
/// Sơ đồ ghế của một suất chiếu cho FE vẽ: ShowtimeId để ở ngoài, mỗi ghế chỉ gồm Id ô (gửi lên khi đặt vé),
/// hàng, số ghế và trạng thái. Không trả ReservationId/RowVersion (chi tiết nội bộ, ai đặt ghế là riêng tư).
/// </summary>
public record SeatMapDto(int ShowtimeId, List<SeatStatusDto> Seats);

public record SeatStatusDto(int Id, string Row, int Number, SeatStatus Status);
