using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Reservations.DTOs;

namespace MovieReservation.Application.Features.Reservations.GetById;

/// <summary>
/// Chi tiết một đơn. Điều kiện chủ đơn nằm ngay trong WHERE: đơn của người khác và đơn không tồn tại
/// cùng ra "không có dòng nào" → cùng 404, kẻ dò Id không phân biệt được (chống IDOR).
/// </summary>
public class GetReservationByIdQueryHandler(IAppDbContext db, TimeProvider timeProvider)
    : IRequestHandler<GetReservationByIdQuery, ReservationDto>
{
    public async Task<ReservationDto> Handle(GetReservationByIdQuery request, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return await db.Reservations
            .AsNoTracking()
            .Where(r => r.Id == request.ReservationId && (r.UserId == request.UserId || request.IsAdmin))
            .Select(ReservationProjection.ToDto(now))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException($"Không tìm thấy đơn {request.ReservationId}.");
    }
}
