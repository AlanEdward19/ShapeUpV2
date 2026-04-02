using FluentValidation;

namespace ShapeUp.Features.Training.WeightTracking.UpsertTargetWeight;

public class UpsertTargetWeightCommandValidator : AbstractValidator<UpsertTargetWeightCommand>
{
    public UpsertTargetWeightCommandValidator()
    {
        RuleFor(x => x.TargetWeight)
            .GreaterThan(0)
            .LessThanOrEqualTo(500);
    }
}
