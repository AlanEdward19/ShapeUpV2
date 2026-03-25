namespace ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

using FluentValidation;

public class GetOrCreateUserValidator : AbstractValidator<GetOrCreateUserCommand>
{
    public GetOrCreateUserValidator()
    {
        RuleFor(x => x.FirebaseUid)
            .NotEmpty();

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();
    }
}

