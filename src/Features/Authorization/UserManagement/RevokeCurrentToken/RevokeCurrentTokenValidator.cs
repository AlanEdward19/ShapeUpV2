namespace ShapeUp.Features.Authorization.UserManagement.RevokeCurrentToken;

using FluentValidation;

public class RevokeCurrentTokenValidator : AbstractValidator<RevokeCurrentTokenCommand>
{
    public RevokeCurrentTokenValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0);

        RuleFor(x => x.FirebaseUid)
            .NotEmpty()
            .MaximumLength(128);
    }
}
