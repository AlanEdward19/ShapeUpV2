namespace ShapeUp.Features.Credentials.ResubmitCredential;

using FluentValidation;

public class ResubmitCredentialValidator : AbstractValidator<ResubmitCredentialCommand>
{
    private static readonly string[] Ufs =
    [
        "AC", "AL", "AP", "AM", "BA", "CE", "DF", "ES", "GO", "MA", "MT", "MS", "MG", "PA",
        "PB", "PR", "PE", "PI", "RJ", "RN", "RS", "RO", "RR", "SC", "SP", "SE", "TO"
    ];

    public ResubmitCredentialValidator()
    {
        RuleFor(x => x.CredentialNumber).NotEmpty().MaximumLength(64);
        RuleFor(x => x.IssuingRegion)
            .Must(r => r is not null && Ufs.Contains(r))
            .WithMessage("IssuingRegion must be a Brazilian state code (UF), in upper case.");
    }
}
