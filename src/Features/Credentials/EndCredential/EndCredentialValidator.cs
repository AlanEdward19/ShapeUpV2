namespace ShapeUp.Features.Credentials.EndCredential;

using FluentValidation;

public class EndCredentialValidator : AbstractValidator<EndCredentialCommand>
{
    public EndCredentialValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
