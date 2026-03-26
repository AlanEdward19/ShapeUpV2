namespace ShapeUp.Features.Authorization.Scopes.SyncUserScopes;

using FluentValidation;

public class SyncUserScopesValidator : AbstractValidator<SyncUserScopesCommand>
{
    public SyncUserScopesValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("UserId must be greater than zero.");
    }
}

