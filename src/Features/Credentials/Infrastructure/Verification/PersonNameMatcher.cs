namespace ShapeUp.Features.Credentials.Infrastructure.Verification;

using System.Globalization;
using System.Text;

/// <summary>
/// Compares the account's name with the name the council has. Strong match = same first and last name and every
/// token of the shorter name present in the longer one (so "Maria Souza" matches "Maria da Silva Souza", but
/// "Maria Souza" does not match "Mariana Souza" and a single name never matches).
/// </summary>
public static class PersonNameMatcher
{
    private static readonly HashSet<string> Particles = ["DA", "DE", "DO", "DAS", "DOS", "E"];

    public static bool IsStrongMatch(string? accountName, string? councilName)
    {
        var a = Tokens(accountName);
        var b = Tokens(councilName);
        if (a.Count < 2 || b.Count < 2)
            return false;

        if (a[0] != b[0] || a[^1] != b[^1])
            return false;

        var (shorter, longer) = a.Count <= b.Count ? (a, b) : (b, a);
        return shorter.All(longer.Contains);
    }

    internal static List<string> Tokens(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return [];

        var decomposed = name.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsLetter(c) ? char.ToUpperInvariant(c) : ' ');
        }

        return sb.ToString()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(t => !Particles.Contains(t))
            .ToList();
    }
}
