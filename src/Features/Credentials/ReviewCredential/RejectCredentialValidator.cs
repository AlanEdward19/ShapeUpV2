namespace ShapeUp.Features.Credentials.ReviewCredential;

using FluentValidation;

public class RejectCredentialValidator : AbstractValidator<RejectCredentialCommand>
{
    public RejectCredentialValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
