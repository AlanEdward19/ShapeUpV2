namespace ShapeUp.Features.GymManagement.Gyms.UpdateGym;

using FluentValidation;

public class UpdateGymValidator : AbstractValidator<UpdateGymCommand>
{
    public UpdateGymValidator()
    {
        RuleFor(x => x.GymId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
        RuleFor(x => x.PlatformTierId).GreaterThan(0).When(x => x.PlatformTierId.HasValue);
    }
}

