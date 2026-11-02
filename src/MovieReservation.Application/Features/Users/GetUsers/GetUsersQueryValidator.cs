using FluentValidation;

namespace MovieReservation.Application.Features.Users.GetUsers;

/// <summary>
/// Kiểm tra tham số phân trang và độ dài chuỗi tìm kiếm của danh sách user (sai → 400).
/// </summary>
public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
{
    public GetUsersQueryValidator()
    {
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Chuỗi tìm kiếm tối đa 100 ký tự.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, GetUsersQuery.MaxPageSize)
            .WithMessage($"PageSize phải từ 1 đến {GetUsersQuery.MaxPageSize}.");
    }
}
