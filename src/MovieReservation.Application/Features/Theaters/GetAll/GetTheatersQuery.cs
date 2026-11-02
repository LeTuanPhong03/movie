using MediatR;
using MovieReservation.Application.Features.Theaters.DTOs;

namespace MovieReservation.Application.Features.Theaters.GetAll;

/// <summary>
/// Liệt kê mọi phòng chiếu (để Admin chọn phòng khi tạo suất). Không phân trang: một rạp chỉ có vài chục phòng.
/// </summary>
public record GetTheatersQuery : IRequest<List<TheaterDto>>;
