using FluentValidation;

namespace MovieReservation.Application.Features.Movies;

/// <summary>
/// Bộ luật kiểm tra dữ liệu phim viết một lần, được Create và Update dùng lại qua Include(...).
/// Độ dài khớp với MovieConfiguration.
/// </summary>
public class MovieInputValidator : AbstractValidator<IMovieInput>
{
    public const int MaxDurationMinutes = 600;
    public const int MaxSuitableAge = 18;

    public MovieInputValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Tên phim bắt buộc phải nhập.")
            .MaximumLength(200).WithMessage("Tên phim có độ dài tối đa là 200 ký tự.");
        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Mô tả có độ dài tối đa là 2000 ký tự.");
        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, MaxDurationMinutes)
            .WithMessage($"Thời lượng phim phải từ 1 đến {MaxDurationMinutes} phút.");
        RuleFor(x => x.Genre)
            .NotEmpty().WithMessage("Thể loại bắt buộc phải nhập.")
            .MaximumLength(50).WithMessage("Thể loại có độ dài tối đa là 50 ký tự.");
        RuleFor(x => x.SuitableAge)
            .InclusiveBetween(0, MaxSuitableAge)
            .WithMessage($"Độ tuổi phù hợp phải từ 0 đến {MaxSuitableAge}.");
        RuleFor(x => x.PosterUrl)
            .MaximumLength(500).WithMessage("Link poster có độ dài tối đa là 500 ký tự.")
            .Must(BeHttpUrl).WithMessage("Link poster phải là URL http/https hợp lệ.")
            .When(x => !string.IsNullOrWhiteSpace(x.PosterUrl));
    }

    private static bool BeHttpUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
