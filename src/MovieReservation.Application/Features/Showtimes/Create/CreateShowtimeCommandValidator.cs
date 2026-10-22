using FluentValidation;

namespace MovieReservation.Application.Features.Showtimes.Create;
/// <summary>
/// Validator cho CreateShowtimeCommand. Kiểm tra các điều kiện cơ bản: MovieId, StartAt, TheaterId, Price. Không kiểm tra trùng lịch chiếu vì cần truy vấn database nên được thực hiện trong Handler.
/// </summary>
public class CreateShowtimeCommandValidator : AbstractValidator<CreateShowtimeCommand>
{
    public CreateShowtimeCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.MovieId)
            .NotEmpty().WithMessage("MovieId bắt buộc nhập.");
        
        RuleFor(x => x.StartAt)
            .NotEmpty().WithMessage("StartAt bắt buộc nhập.")
            .Must(start => start.Kind == DateTimeKind.Utc).WithMessage("StartAt phải là giờ UTC (kết thúc bằng Z).")
            .Must(start => start > timeProvider.GetUtcNow().UtcDateTime).WithMessage("StartAt phải ở tương lai.");
        RuleFor(x => x.TheaterId)
            .NotEmpty().WithMessage("TheaterId bắt buộc nhập.");
        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price phải lớn hơn 0.");
    }
}