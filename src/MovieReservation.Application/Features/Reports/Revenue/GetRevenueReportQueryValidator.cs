using FluentValidation;

namespace MovieReservation.Application.Features.Reports.Revenue;

/// <summary>
/// Kiểm tra khoảng ngày của báo cáo: From ≤ To và không quá MaxDays ngày (chặn query quét cả bảng vì gõ nhầm năm).
/// </summary>
public class GetRevenueReportQueryValidator : AbstractValidator<GetRevenueReportQuery>
{
    public GetRevenueReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("From không được để trống.");
        RuleFor(x => x.To).NotEmpty().WithMessage("To không được để trống.");
        RuleFor(x => x)
            .Must(x => x.From <= x.To).WithMessage("From phải trước hoặc bằng To.")
            .Must(x => x.To.DayNumber - x.From.DayNumber < GetRevenueReportQuery.MaxDays)
                .WithMessage($"Khoảng báo cáo tối đa {GetRevenueReportQuery.MaxDays} ngày.");
        RuleFor(x => x.GroupBy).IsInEnum().WithMessage("GroupBy phải là Day hoặc Movie.");
    }
}
