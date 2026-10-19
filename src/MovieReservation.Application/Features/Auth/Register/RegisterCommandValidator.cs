using FluentValidation;

namespace MovieReservation.Application.Features.Auth.Register;
/// <summary>
/// Validator cho RegisterCommand, sử dụng FluentValidation.
/// </summary>
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full Name bắt buộc phải nhập.")
            .MaximumLength(100).WithMessage("Full Name có độ dài tối đa là 100 ký tự.");
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email bắt buộc phải nhập.").MaximumLength(256).WithMessage("Email có độ dài tối đa là 256 ký tự.")
            .EmailAddress().WithMessage("Email không hợp lệ.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password bắt buộc phải nhập.")
            .MinimumLength(8).WithMessage("Password có độ dài tối thiểu là 8 ký tự.")
            .MaximumLength(72).WithMessage("Password có độ dài tối đa là 72 ký tự.");
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm Password bắt buộc phải nhập.")
            .Equal(x => x.Password).WithMessage("Password không khớp.");
    }
}