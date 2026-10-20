using FluentValidation;

namespace MovieReservation.Application.Features.Movies.GetMovies;

/// <summary>
/// Kiểm tra tham số phân trang. Gửi sai thì báo 400, không âm thầm cắt về giới hạn (docs/decisions.md mục 5).
/// </summary>
public class GetMoviesQueryValidator : AbstractValidator<GetMoviesQuery>
{
    public GetMoviesQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page phải từ 1 trở lên.");
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, GetMoviesQuery.MaxPageSize)
            .WithMessage($"PageSize phải từ 1 đến {GetMoviesQuery.MaxPageSize}.");
    }
}
