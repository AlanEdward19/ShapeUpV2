namespace ShapeUp.Features.Credentials.ReviewCredential;

using FluentValidation;

public class ApproveCredentialValidator : AbstractValidator<ApproveCredentialCommand>
{
    public const int MaxValidityYears = 5;

    public ApproveCredentialValidator()
    {
        RuleFor(x => x.ExpiresAt)
            .Must(d => d!.Value.ToUniversalTime() > DateTime.UtcNow)
            .WithMessage("ExpiresAt must be in the future.")
            .Must(d => d!.Value.ToUniversalTime() <= DateTime.UtcNow.AddYears(MaxValidityYears))
            .WithMessage($"ExpiresAt cannot be more than {MaxValidityYears} years from now.")
            .When(x => x.ExpiresAt.HasValue);
    }
}
