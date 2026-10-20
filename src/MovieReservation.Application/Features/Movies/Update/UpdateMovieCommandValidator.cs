using FluentValidation;

namespace MovieReservation.Application.Features.Movies.Update;

/// <summary>
/// Kiểm tra input của UpdateMovieCommand bằng bộ luật chung MovieInputValidator.
/// </summary>
public class UpdateMovieCommandValidator : AbstractValidator<UpdateMovieCommand>
{
    public UpdateMovieCommandValidator()
    {
        Include(new MovieInputValidator());
    }
}
