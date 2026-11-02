using FluentValidation;

namespace MovieReservation.Application.Features.Reservations.GetAll;

/// <summary>
/// Kiểm tra bộ lọc và phân trang của danh sách đơn cho Admin (sai → 400).
/// </summary>
public class GetAllReservationsQueryValidator : AbstractValidator<GetAllReservationsQuery>
{
    public GetAllReservationsQueryValidator()
    {
        RuleFor(x => x.ShowtimeId).GreaterThan(0).When(x => x.ShowtimeId.HasValue).WithMessage("ShowtimeId không hợp lệ.");
        RuleFor(x => x.UserId).GreaterThan(0).When(x => x.UserId.HasValue).WithMessage("UserId không hợp lệ.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái không hợp lệ.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, GetAllReservationsQuery.MaxPageSize)
            .WithMessage($"PageSize phải từ 1 đến {GetAllReservationsQuery.MaxPageSize}.");
    }
}
