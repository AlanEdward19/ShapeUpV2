using FluentValidation;

namespace ShapeUp.Features.GymManagement.GymPlans.UpdateGymPlan;

public class UpdateGymPlanValidator : AbstractValidator<UpdateGymPlanCommand>
{
    public UpdateGymPlanValidator()
    {
        RuleFor(x => x.PlanId).GreaterThan(0);
        RuleFor(x => x.GymId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationDays).GreaterThan(0);
    }
}