using FluentValidation;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.Profile.SetDietaryRestrictions;

public class SetDietaryRestrictionsCommandValidator : AbstractValidator<SetDietaryRestrictionsCommand>
{
    public SetDietaryRestrictionsCommandValidator()
    {
        RuleFor(x => x.Restrictions).MaximumLength(1000);
        RuleFor(x => x.Allergies).MaximumLength(1000);
    }
}
