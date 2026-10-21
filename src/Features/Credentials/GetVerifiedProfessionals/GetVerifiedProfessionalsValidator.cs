namespace ShapeUp.Features.Credentials.GetVerifiedProfessionals;

using FluentValidation;
using Shared.Entities;

public class GetVerifiedProfessionalsValidator : AbstractValidator<GetVerifiedProfessionalsQuery>
{
    public GetVerifiedProfessionalsValidator()
    {
        RuleFor(x => x.ProfessionType)
            .Must(profession => string.IsNullOrWhiteSpace(profession)
                || profession.Trim() is CredentialAuthorities.PersonalTrainer or CredentialAuthorities.Nutritionist)
            .WithMessage("ProfessionType must be PersonalTrainer or Nutritionist.");
    }
}
