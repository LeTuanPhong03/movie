using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Theaters.DTOs;

namespace MovieReservation.Application.Features.Theaters.GetById;

/// <summary>
/// Đọc phòng chiếu theo Id qua TheaterProjection (Rows/SeatsPerRow tính lại từ ghế); không có → 404.
/// </summary>
public class GetTheaterByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetTheaterByIdQuery, TheaterDto>
{
    public async Task<TheaterDto> Handle(GetTheaterByIdQuery request, CancellationToken cancellationToken)
    {
        return await db.Theaters
            .AsNoTracking()
            .Where(t => t.Id == request.Id)
            .Select(TheaterProjection.ToDto)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy phòng chiếu {request.Id}.");
    }
}
