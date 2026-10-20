using FluentValidation;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Theaters.Create;

/// <summary>
/// Kiểm tra hình dạng input của CreateTheaterCommand (trả 400 kèm thông báo dễ hiểu).
/// Giới hạn lấy từ hằng của Theater để khớp với guard trong Domain.
/// </summary>
public class CreateTheaterCommandValidator : AbstractValidator<CreateTheaterCommand>
{
    public CreateTheaterCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tên phòng bắt buộc phải nhập.")
            .MaximumLength(100).WithMessage("Tên phòng có độ dài tối đa là 100 ký tự.");
        RuleFor(x => x.Rows)
            .InclusiveBetween(1, Theater.MaxRows)
            .WithMessage($"Số hàng phải từ 1 đến {Theater.MaxRows}.");
        RuleFor(x => x.SeatsPerRow)
            .InclusiveBetween(1, Theater.MaxSeatsPerRow)
            .WithMessage($"Số ghế mỗi hàng phải từ 1 đến {Theater.MaxSeatsPerRow}.");
    }
}
