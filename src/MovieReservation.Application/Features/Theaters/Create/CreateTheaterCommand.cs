using MediatR;
using MovieReservation.Application.Features.Theaters.DTOs;

namespace MovieReservation.Application.Features.Theaters.Create;

/// <summary>
/// Yêu cầu tạo phòng chiếu kèm sơ đồ ghế hình chữ nhật: Rows hàng (A, B, ...) × SeatsPerRow ghế mỗi hàng.
/// </summary>
public record CreateTheaterCommand(string Name, int Rows, int SeatsPerRow) : IRequest<TheaterDto>;
