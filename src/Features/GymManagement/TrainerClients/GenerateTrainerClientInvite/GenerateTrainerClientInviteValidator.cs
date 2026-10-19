namespace ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;

using FluentValidation;
using ShapeUp.Shared.Invites;

public class GenerateTrainerClientInviteValidator : AbstractValidator<GenerateTrainerClientInviteCommand>
{
    public GenerateTrainerClientInviteValidator()
    {
        RuleFor(command => command.GetClientEmail())
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.GetTrainerName())
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.TrainerPlanId)
            .GreaterThan(0)
            .When(command => command.TrainerPlanId.HasValue);

        RuleFor(command => command.ExpiresInHours)
            .InclusiveBetween(InviteRules.MinLifetimeHours, InviteRules.MaxLifetimeHours)
            .When(command => command.ExpiresInHours.HasValue);
    }
}


