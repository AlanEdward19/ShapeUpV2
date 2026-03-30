namespace ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;

using FluentValidation;
using Shared.Entities;

public class CreatePlatformTierValidator : AbstractValidator<CreatePlatformTierCommand>
{
    public CreatePlatformTierValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.TargetRole).IsInEnum();
        RuleFor(x => x.TargetRole)
            .Must(role => role is PlatformRoleType.Trainer or PlatformRoleType.IndependentClient or PlatformRoleType.GymOwner)
            .WithMessage("TargetRole can only be Trainer, IndependentClient, or GymOwner.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MaxClients).GreaterThan(0).When(x => x.MaxClients.HasValue);
        RuleFor(x => x.MaxTrainers).GreaterThan(0).When(x => x.MaxTrainers.HasValue);
    }
}

