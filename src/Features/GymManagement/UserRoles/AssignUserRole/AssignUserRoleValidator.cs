namespace ShapeUp.Features.GymManagement.UserRoles.AssignUserRole;

using FluentValidation;
using ShapeUp.Features.GymManagement.Shared.Entities;

public class AssignUserRoleValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Role).IsInEnum();
        RuleFor(x => x.PlatformTierId).GreaterThan(0).When(x => x.PlatformTierId.HasValue);
    }
}

