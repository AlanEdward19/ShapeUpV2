using FluentValidation;

namespace ShapeUp.Features.Authorization.UserManagement.SetUserActive;

public class SetUserActiveValidator : AbstractValidator<SetUserActiveCommand>
{
    public SetUserActiveValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.ActorUserId).GreaterThan(0);
    }
}
