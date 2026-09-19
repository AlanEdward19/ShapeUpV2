using FluentValidation;

namespace ShapeUp.Features.Nutrition.Fasting.GetHistory;

public sealed class GetFastingHistoryQueryValidator : AbstractValidator<GetFastingHistoryQuery>
{
    public const int DefaultPageSize = 14;
    public const int MaxPageSize = 14;

    public GetFastingHistoryQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, MaxPageSize)
            .When(x => x.PageSize.HasValue);
    }
}
