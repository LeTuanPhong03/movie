using FluentValidation;

namespace MovieReservation.Application.Features.Showtimes.GetSeats;

/// <summary>
/// Kiểm tra hình dạng của yêu cầu xem sơ đồ ghế: ShowtimeId phải lớn hơn 0. Suất có tồn tại hay không thì handler kiểm tra (cần DB).
/// </summary>
public class GetSeatsByShowtimeQueryValidator : AbstractValidator<GetSeatsByShowtimeQuery>
{
    public GetSeatsByShowtimeQueryValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .GreaterThan(0).WithMessage("ShowtimeId phải lớn hơn 0.");
    }
}
