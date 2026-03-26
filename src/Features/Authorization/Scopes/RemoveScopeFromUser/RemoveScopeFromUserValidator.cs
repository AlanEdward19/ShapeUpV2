namespace ShapeUp.Features.Authorization.Scopes.RemoveScopeFromUser;

using FluentValidation;

public class RemoveScopeFromUserValidator : AbstractValidator<RemoveScopeFromUserCommand>
{
    public RemoveScopeFromUserValidator()
    {
        RuleFor(x => x.ScopeId)
            .GreaterThan(0)
            .WithMessage("ScopeId must be greater than zero.");
    }
}

