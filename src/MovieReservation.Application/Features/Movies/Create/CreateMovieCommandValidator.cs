using FluentValidation;

namespace MovieReservation.Application.Features.Movies.Create;

/// <summary>
/// Kiểm tra input của CreateMovieCommand bằng bộ luật chung MovieInputValidator.
/// </summary>
public class CreateMovieCommandValidator : AbstractValidator<CreateMovieCommand>
{
    public CreateMovieCommandValidator()
    {
        Include(new MovieInputValidator());
    }
}
