using FluentValidation;

namespace ShapeUp.Features.Training.WeightTracking.GetWeightRegisters;

public class GetWeightRegistersQueryValidator : AbstractValidator<GetWeightRegistersQuery>
{
    public GetWeightRegistersQueryValidator()
    {
        RuleFor(x => x.StartDateUtc).NotEmpty();
        RuleFor(x => x.EndDateUtc).NotEmpty();
        RuleFor(x => x.EndDateUtc)
            .GreaterThanOrEqualTo(x => x.StartDateUtc)
            .WithMessage("EndDateUtc must be greater than or equal to StartDateUtc.");
    }
}
