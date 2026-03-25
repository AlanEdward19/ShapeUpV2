namespace ShapeUp.Features.GymManagement.Gyms.CreateGym;

using FluentValidation;

public class CreateGymValidator : AbstractValidator<CreateGymCommand>
{
    public CreateGymValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address != null);
        RuleFor(x => x.PlatformTierId).GreaterThan(0).When(x => x.PlatformTierId.HasValue);
    }
}

