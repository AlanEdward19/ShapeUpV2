namespace ShapeUp.Features.Authorization.Scopes.SyncCurrentUserScopes;

using FluentValidation;

public class SyncCurrentUserScopesValidator : AbstractValidator<SyncCurrentUserScopesCommand>
{
    public SyncCurrentUserScopesValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId must be greater than zero.");
    }
}

