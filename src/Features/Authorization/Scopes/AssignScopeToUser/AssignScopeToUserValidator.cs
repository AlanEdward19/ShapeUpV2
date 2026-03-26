namespace ShapeUp.Features.Authorization.Scopes.AssignScopeToUser;

using FluentValidation;

public class AssignScopeToUserValidator : AbstractValidator<AssignScopeToUserCommand>
{
    public AssignScopeToUserValidator()
    {
        RuleFor(x => x.ScopeId)
            .GreaterThan(0)
            .WithMessage("ScopeId must be greater than zero.");
    }
}

