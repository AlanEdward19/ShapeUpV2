using FluentValidation;

namespace ShapeUp.Features.Authorization.UserManagement.MobileExperience;

public class RegisterMobileExperienceValidator : AbstractValidator<RegisterMobileExperienceCommand>
{
    public RegisterMobileExperienceValidator() => RuleFor(x => x.Platform)
        .Must(platform => platform is "android" or "ios")
        .WithMessage("A plataforma deve ser android ou ios.");
}
