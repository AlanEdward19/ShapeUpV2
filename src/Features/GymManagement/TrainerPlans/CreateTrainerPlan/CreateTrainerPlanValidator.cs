using FluentValidation;

namespace ShapeUp.Features.GymManagement.TrainerPlans.CreateTrainerPlan;

public class CreateTrainerPlanValidator : AbstractValidator<CreateTrainerPlanCommand>
{
    public CreateTrainerPlanValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.DurationDays).GreaterThan(0);
    }
}