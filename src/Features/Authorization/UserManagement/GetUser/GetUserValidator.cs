using FluentValidation;

namespace ShapeUp.Features.Authorization.UserManagement.GetUser;

public class GetUserValidator : AbstractValidator<GetUserQuery>
{
    public GetUserValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}

