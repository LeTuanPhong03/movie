using FluentValidation;
using MovieReservation.Domain.Entities;

namespace MovieReservation.Application.Features.Reservations.Create;

/// <summary>
/// Kiểm tra hình dạng request đặt vé (không cần DB → 400): có ít nhất 1 ghế, tối đa Reservation.MaxSeats ghế,
/// không gửi trùng ghế (trùng sẽ làm tính sai tiền / báo sai "ghế không tồn tại"), mọi Id phải > 0.
/// Ghế còn trống hay không là chuyện của dữ liệu → kiểm tra ở handler.
/// </summary>
public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    public CreateReservationCommandValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .GreaterThan(0).WithMessage("ShowtimeId phải lớn hơn 0.");

        RuleFor(x => x.SeatIds)
            .NotEmpty().WithMessage("Phải chọn ít nhất 1 ghế.")
            .Must(ids => ids.Count <= Reservation.MaxSeats)
                .WithMessage($"Mỗi lần đặt tối đa {Reservation.MaxSeats} ghế.")
            .Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Danh sách ghế bị trùng.");

        RuleForEach(x => x.SeatIds)
            .GreaterThan(0).WithMessage("Id ghế phải lớn hơn 0.");
    }
}
