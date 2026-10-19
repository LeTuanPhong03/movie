using FluentValidation;

namespace MovieReservation.Application.Features.Auth.Logout;

/// <summary>
/// Luật đầu vào của đăng xuất: bắt buộc có refresh token.
/// </summary>
public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token bắt buộc phải có.");
    }
}
