using FluentValidation;

namespace ShapeUp.Features.GymManagement.GymClients.EnrollGymClient;

public class EnrollGymClientValidator : AbstractValidator<EnrollGymClientCommand>
{
    public EnrollGymClientValidator()
    {
        RuleFor(x => x.GymId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
        RuleFor(x => x.GymPlanId).GreaterThan(0);
        RuleFor(x => x.TrainerId).GreaterThan(0).When(x => x.TrainerId.HasValue);
    }
}