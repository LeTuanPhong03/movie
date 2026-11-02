using FluentValidation;

namespace MovieReservation.Application.Features.Users.ChangeRole;

/// <summary>
/// Kiểm tra Id user hợp lệ và Role là giá trị có trong enum (gửi "SuperAdmin" hay số 99 → 400).
/// </summary>
public class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0).WithMessage("UserId không hợp lệ.");
        RuleFor(x => x.Role).IsInEnum().WithMessage("Role phải là Admin hoặc Customer.");
    }
}
