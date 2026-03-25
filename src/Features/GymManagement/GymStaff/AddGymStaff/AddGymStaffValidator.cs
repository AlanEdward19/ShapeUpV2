using FluentValidation;

namespace ShapeUp.Features.GymManagement.GymStaff.AddGymStaff;

public class AddGymStaffValidator : AbstractValidator<AddGymStaffCommand>
{
    public AddGymStaffValidator()
    {
        RuleFor(x => x.GymId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.Role).IsInEnum();
    }
}