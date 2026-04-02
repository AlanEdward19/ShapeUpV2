using FluentValidation;

namespace ShapeUp.Features.GymManagement.TrainerClients.UnassignTrainerClient;

public class UnassignTrainerClientValidator : AbstractValidator<UnassignTrainerClientCommand>
{
    public UnassignTrainerClientValidator()
    {
        RuleFor(x => x.ClientId).GreaterThan(0);
    }
}

