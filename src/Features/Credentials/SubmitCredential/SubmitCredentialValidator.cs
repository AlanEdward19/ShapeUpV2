namespace ShapeUp.Features.Credentials.SubmitCredential;

using FluentValidation;
using Shared.Entities;

public class SubmitCredentialValidator : AbstractValidator<SubmitCredentialCommand>
{
    private static readonly string[] Ufs =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    public SubmitCredentialValidator()
    {
        RuleFor(x => x.ProfessionType)
            .Must(p => p is CredentialAuthorities.PersonalTrainer or CredentialAuthorities.Nutritionist)
            .WithMessage("ProfessionType must be PersonalTrainer or Nutritionist.");

        RuleFor(x => x.CredentialNumber)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.IssuingAuthority)
            .Must(a => a is CredentialAuthorities.Cref or CredentialAuthorities.Crn)
            .WithMessage("IssuingAuthority must be CREF or CRN.");

        RuleFor(x => x)
            .Must(x => CredentialAuthorities.AuthorityFor(x.ProfessionType) == x.IssuingAuthority)
            .When(x => CredentialAuthorities.AuthorityFor(x.ProfessionType) is not null
                       && x.IssuingAuthority is CredentialAuthorities.Cref or CredentialAuthorities.Crn)
            .WithName(nameof(SubmitCredentialCommand.IssuingAuthority))
            .WithMessage("PersonalTrainer is registered with CREF and Nutritionist with CRN.");

        RuleFor(x => x.IssuingRegion)
            .Must(r => r is not null && Ufs.Contains(r))
            .WithMessage("IssuingRegion must be a Brazilian state code (UF), in upper case.");

        // Only the Brazilian councils are supported for now.
        RuleFor(x => x.Country)
            .Must(c => c == "BR")
            .WithMessage("Country must be BR; only Brazilian councils are supported.");
    }
}
