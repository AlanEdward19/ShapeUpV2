namespace ShapeUp.Features.Authorization.Groups.AddUserToGroup;

using FluentValidation;

public class AddUserToGroupValidator : AbstractValidator<AddUserToGroupCommand>
{
    public AddUserToGroupValidator()
    {
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.GroupId).GreaterThan(0);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(role => role.Equals("Owner", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Administrator", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Member", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Invalid role. Must be one of: Owner, Administrator, Member.");
    }
}

