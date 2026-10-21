using FluentValidation;

namespace ShapeUp.Features.Authorization.UserManagement.ListUsers;

public class ListUsersValidator : AbstractValidator<ListUsersQuery>
{
    public ListUsersValidator()
    {
        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Search));
    }
}
