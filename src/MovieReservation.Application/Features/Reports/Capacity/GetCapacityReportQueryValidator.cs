using FluentValidation;

namespace MovieReservation.Application.Features.Reports.Capacity;

/// <summary>
/// Kiểm tra khoảng ngày của báo cáo công suất: From ≤ To và không quá MaxDays ngày
/// (báo cáo liệt kê từng suất nên giới hạn chặt hơn báo cáo doanh thu).
/// </summary>
public class GetCapacityReportQueryValidator : AbstractValidator<GetCapacityReportQuery>
{
    public GetCapacityReportQueryValidator()
    {
        RuleFor(x => x.From).NotEmpty().WithMessage("From không được để trống.");
        RuleFor(x => x.To).NotEmpty().WithMessage("To không được để trống.");
        RuleFor(x => x)
            .Must(x => x.From <= x.To).WithMessage("From phải trước hoặc bằng To.")
            .Must(x => x.To.DayNumber - x.From.DayNumber < GetCapacityReportQuery.MaxDays)
                .WithMessage($"Khoảng báo cáo tối đa {GetCapacityReportQuery.MaxDays} ngày.");
    }
}
