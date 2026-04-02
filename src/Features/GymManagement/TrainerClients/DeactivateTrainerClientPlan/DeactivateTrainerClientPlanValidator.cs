using FluentValidation;

namespace ShapeUp.Features.GymManagement.TrainerClients.DeactivateTrainerClientPlan;

public class DeactivateTrainerClientPlanValidator : AbstractValidator<DeactivateTrainerClientPlanCommand>
{
    public DeactivateTrainerClientPlanValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
    }
}

