namespace ShapeUp.Features.GymManagement.GymPlans.CreateGymPlan;

using FluentValidation;

public class CreateGymPlanValidator : AbstractValidator<CreateGymPlanCommand>
{
    public CreateGymPlanValidator()
    {
        RuleFor(x => x.GymId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationDays).GreaterThan(0);
    }
}

