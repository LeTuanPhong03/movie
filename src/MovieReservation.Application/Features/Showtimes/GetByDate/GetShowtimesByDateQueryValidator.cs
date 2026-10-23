using FluentValidation;

namespace MovieReservation.Application.Features.Showtimes.GetByDate;
/// <summary>
/// Validator cho GetShowtimesByDateQuery : kiểm tra ngày không được để trống.
/// </summary>
public class GetShowtimesByDateQueryValidator : AbstractValidator<GetShowtimesByDateQuery>
{
    public GetShowtimesByDateQueryValidator()
    {
        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Ngày không được để trống.");
    }
}