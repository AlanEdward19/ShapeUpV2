using FluentValidation;

namespace ShapeUp.Features.Nutrition.Moderation.GetPendingModerations;

public class GetPendingModerationsQueryValidator : AbstractValidator<GetPendingModerationsQuery>
{
    public GetPendingModerationsQueryValidator()
    {
        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .When(x => x.PageSize.HasValue);
    }
}
