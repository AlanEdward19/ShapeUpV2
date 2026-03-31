using FluentValidation;

namespace ShapeUp.Features.GymManagement.TrainerClients.AddTrainerClient;

public class AddTrainerClientValidator : AbstractValidator<AddTrainerClientCommand>
{
    public AddTrainerClientValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
        RuleFor(x => x.TrainerPlanId)
            .GreaterThan(0)
            .When(x => x.TrainerPlanId.HasValue);
    }
}