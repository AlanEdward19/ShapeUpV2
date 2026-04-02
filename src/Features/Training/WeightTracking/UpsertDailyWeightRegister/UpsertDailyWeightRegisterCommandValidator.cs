using FluentValidation;

namespace ShapeUp.Features.Training.WeightTracking.UpsertDailyWeightRegister;

public class UpsertDailyWeightRegisterCommandValidator : AbstractValidator<UpsertDailyWeightRegisterCommand>
{
    public UpsertDailyWeightRegisterCommandValidator()
    {
        RuleFor(x => x.Weight)
            .GreaterThan(0)
            .LessThanOrEqualTo(500);
    }
}
