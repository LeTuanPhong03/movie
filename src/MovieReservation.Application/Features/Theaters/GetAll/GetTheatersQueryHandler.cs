using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Theaters.DTOs;

namespace MovieReservation.Application.Features.Theaters.GetAll;

/// <summary>
/// Danh sách phòng chiếu sắp theo Id, chiếu qua TheaterProjection (đếm ghế trong SQL, không tải ghế về).
/// </summary>
public class GetTheatersQueryHandler(IAppDbContext db) : IRequestHandler<GetTheatersQuery, List<TheaterDto>>
{
    public async Task<List<TheaterDto>> Handle(GetTheatersQuery request, CancellationToken cancellationToken)
    {
        return await db.Theaters
            .AsNoTracking()
            .OrderBy(t => t.Id)
            .Select(TheaterProjection.ToDto)
            .ToListAsync(cancellationToken);
    }
}
