namespace ShapeUp.Features.Credentials.ReviewCredential;

using FluentValidation;

public class RequestChangesValidator : AbstractValidator<RequestChangesCommand>
{
    public RequestChangesValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(500);
    }
}
