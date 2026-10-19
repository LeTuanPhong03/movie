using FluentValidation;

namespace MovieReservation.Application.Features.Auth.Refresh;

/// <summary>
/// Luật đầu vào của làm mới token: bắt buộc có refresh token.
/// </summary>
public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token bắt buộc phải có.");
    }
}
