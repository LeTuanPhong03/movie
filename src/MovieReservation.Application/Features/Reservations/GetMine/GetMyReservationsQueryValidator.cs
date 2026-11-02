using FluentValidation;

namespace MovieReservation.Application.Features.Reservations.GetMine;

/// <summary>
/// Kiểm tra tham số phân trang và trạng thái lọc của lịch sử đơn (sai → 400, giống GetMoviesQueryValidator).
/// </summary>
public class GetMyReservationsQueryValidator : AbstractValidator<GetMyReservationsQuery>
{
    public GetMyReservationsQueryValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage("Trạng thái không hợp lệ.");
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, GetMyReservationsQuery.MaxPageSize)
            .WithMessage($"PageSize phải từ 1 đến {GetMyReservationsQuery.MaxPageSize}.");
    }
}
