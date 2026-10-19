namespace ShapeUp.Shared.Safety;

using System.Globalization;
using System.Text;

/// <summary>
/// First line of defense for user-written text that other people read (App Store 1.2 asks for a way to filter objectionable
/// material). It matches whole words after removing accents and case, so "classe" is not caught by a rule about "ass".
/// Reports and blocking cover what a list cannot.
/// </summary>
public static class ObjectionableText
{
    private static readonly HashSet<string> Words = new(StringComparer.Ordinal)
    {
        "porra", "caralho", "puta", "puto", "viado", "viada", "buceta", "foder", "fodase", "foda-se", "arrombado", "arrombada",
        "desgracado", "otario", "otaria", "babaca", "retardado", "retardada", "vagabundo", "vagabunda", "cuzao", "merda",
        "nazista", "estupro", "estuprar", "fuck", "shit", "bitch", "asshole", "nigger", "faggot", "cunt", "whore", "rape"
    };

    public static bool Contains(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        foreach (var token in Tokens(text))
            if (Words.Contains(token))
                return true;
        return false;
    }

    private static IEnumerable<string> Tokens(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetter(c) || c == '-')
            {
                sb.Append(char.ToLowerInvariant(c));
            }
            else if (sb.Length > 0)
            {
                yield return sb.ToString();
                sb.Clear();
            }
        }

        if (sb.Length > 0)
            yield return sb.ToString();
    }
}
