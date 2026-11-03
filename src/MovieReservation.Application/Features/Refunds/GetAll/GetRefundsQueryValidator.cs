using FluentValidation;
using MovieReservation.Domain.Enums;

namespace MovieReservation.Application.Features.Refunds.GetAll;

/// <summary>
/// Chỉ cho lọc RefundRequired hoặc Refunded (endpoint này không phải chỗ xem mọi thanh toán) và kiểm phân trang.
/// </summary>
public class GetRefundsQueryValidator : AbstractValidator<GetRefundsQuery>
{
    public GetRefundsQueryValidator()
    {
        RuleFor(x => x.Status)
            .Must(s => s is PaymentStatus.RefundRequired or PaymentStatus.Refunded)
            .WithMessage("Status phải là RefundRequired hoặc Refunded.");
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, GetRefundsQuery.MaxPageSize)
            .WithMessage($"PageSize phải từ 1 đến {GetRefundsQuery.MaxPageSize}.");
    }
}
