namespace ShapeUp.Features.Authorization.UserManagement.GetOrCreateUser;

using FluentValidation;

public class GetUserValidator : AbstractValidator<GetUserQuery>
{
    public GetUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

