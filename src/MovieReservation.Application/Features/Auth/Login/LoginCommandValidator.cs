using FluentValidation;

namespace MovieReservation.Application.Features.Auth.Login;
/// <summary>
/// Validator cho LoginCommand, sử dụng FluentValidation.
/// </summary>

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email bắt buộc phải nhập.")
            .EmailAddress().WithMessage("Email không hợp lệ.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password bắt buộc phải nhập.");
    }
}