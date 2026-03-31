namespace ShapeUp.Features.GymManagement.TrainerClients.GenerateTrainerClientInvite;

using FluentValidation;

public class GenerateTrainerClientInviteValidator : AbstractValidator<GenerateTrainerClientInviteCommand>
{
    public GenerateTrainerClientInviteValidator()
    {
        RuleFor(command => command.ClientEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(320);

        RuleFor(command => command.TrainerName)
            .NotEmpty()
            .MaximumLength(120);

        RuleFor(command => command.TrainerPlanId)
            .GreaterThan(0)
            .When(command => command.TrainerPlanId.HasValue);

        RuleFor(command => command.ExpiresInHours)
            .InclusiveBetween(1, 168)
            .When(command => command.ExpiresInHours.HasValue);
    }
}


