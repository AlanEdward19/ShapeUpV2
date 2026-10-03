namespace ShapeUp.Features.Credentials.Shared.Entities;

/// <summary>Councils accepted today and the profession each one registers.</summary>
public static class CredentialAuthorities
{
    public const string PersonalTrainer = "PersonalTrainer";
    public const string Nutritionist = "Nutritionist";
    public const string Cref = "CREF";
    public const string Crn = "CRN";

    public static string? AuthorityFor(string professionType) => professionType switch
    {
        PersonalTrainer => Cref,
        Nutritionist => Crn,
        _ => null
    };
}
