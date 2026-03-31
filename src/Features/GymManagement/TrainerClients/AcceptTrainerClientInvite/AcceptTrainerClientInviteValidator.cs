namespace ShapeUp.Features.GymManagement.TrainerClients.AcceptTrainerClientInvite;

using FluentValidation;

public class AcceptTrainerClientInviteValidator : AbstractValidator<AcceptTrainerClientInviteCommand>
{
    public AcceptTrainerClientInviteValidator()
    {
        RuleFor(command => command.AccessToken)
            .NotEmpty();
    }
}

