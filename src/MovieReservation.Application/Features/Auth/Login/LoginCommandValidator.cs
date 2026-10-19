using FluentValidation;

namespace MovieReservation.Application.Features.Auth.Login;

/// <summary>
/// Luật đầu vào của đăng nhập: chỉ bắt buộc có email và mật khẩu.
/// Không kiểm tra độ dài/định dạng mật khẩu vì luật đăng ký có thể đổi, người dùng cũ vẫn phải đăng nhập được.
/// </summary>
public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email bắt buộc phải nhập.");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu bắt buộc phải nhập.");
    }
}
