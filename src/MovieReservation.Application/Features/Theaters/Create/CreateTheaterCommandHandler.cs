using MediatR;
using Microsoft.EntityFrameworkCore;
using MovieReservation.Application.Common.Exceptions;
using MovieReservation.Application.Common.Interfaces;
using MovieReservation.Application.Features.Theaters.DTOs;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Theaters.Create;

/// <summary>
/// Tạo phòng chiếu: kiểm tra tên trùng (không phân biệt hoa/thường), sinh ghế qua Theater.GenerateSeats,
/// lưu phòng + toàn bộ ghế trong một lần SaveChanges.
/// </summary>
public class CreateTheaterCommandHandler(IAppDbContext db) : IRequestHandler<CreateTheaterCommand, TheaterDto>
{
    public async Task<TheaterDto> Handle(CreateTheaterCommand request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();

        // Kiểm tra rẻ trước (fail fast). Lưu nguyên tên admin gõ để hiển thị, chỉ so sánh bằng chữ thường.
        var nameLower = name.ToLower();
        if (await db.Theaters.AnyAsync(t => t.Name.ToLower() == nameLower, cancellationToken))
        {
            throw new ConflictException("Tên phòng đã tồn tại.");
        }

        var theater = new Theater { Name = name };
        theater.GenerateSeats(request.Rows, request.SeatsPerRow);
        db.Theaters.Add(theater);

        // Ghế nằm trong theater.Seats (navigation) → EF tự điền TheaterId, phòng và ghế cùng một transaction
        await db.SaveChangesAsync(cancellationToken);

        // TotalSeats đếm từ ghế thật đã sinh, không tính lại Rows * SeatsPerRow
        return new TheaterDto(
            Id: theater.Id,
            Name: theater.Name,
            Rows: request.Rows,
            SeatsPerRow: request.SeatsPerRow,
            TotalSeats: theater.Seats.Count);
    }
}
