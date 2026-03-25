namespace ShapeUp.Features.Authorization.Scopes.CreateScope;

using FluentValidation;
using System.Text.RegularExpressions;

public class CreateScopeValidator : AbstractValidator<CreateScopeCommand>
{
    public CreateScopeValidator()
    {
        RuleFor(x => x.Domain)
            .NotEmpty()
            .Must(IsValidScopePart)
            .WithMessage("Domain can only contain alphanumeric characters and underscores.");

        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .Must(IsValidScopePart)
            .WithMessage("Subdomain can only contain alphanumeric characters and underscores.");

        RuleFor(x => x.Action)
            .NotEmpty()
            .Must(IsValidScopePart)
            .WithMessage("Action can only contain alphanumeric characters and underscores.");
    }

    private static bool IsValidScopePart(string part) =>
        Regex.IsMatch(part, @"^[a-zA-Z0-9_]+$");
}

