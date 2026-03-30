namespace ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

using FluentValidation;
using Shared.Entities;

public class AssignUserRoleValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.Role)
            .Must(role => role is PlatformRoleType.Trainer or PlatformRoleType.IndependentClient or PlatformRoleType.GymOwner)
            .WithMessage("Role can only be Trainer, IndependentClient, or GymOwner when assigned manually.");
        RuleFor(x => x.PlatformTierId).GreaterThan(0).When(x => x.PlatformTierId.HasValue);
    }
}

