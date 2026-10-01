using FluentValidation;

namespace ShapeUp.Features.Nutrition.Hydration.GetHydrationRange;

public class GetHydrationRangeQueryValidator : AbstractValidator<GetHydrationRangeQuery>
{
    public const int MaxDays = 366;

    public GetHydrationRangeQueryValidator()
    {
        RuleFor(x => x.To)
            .GreaterThanOrEqualTo(x => x.From)
            .WithMessage("To must be greater than or equal to From.");

        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber < MaxDays)
            .WithMessage($"The range can have at most {MaxDays} days.");
    }
}
