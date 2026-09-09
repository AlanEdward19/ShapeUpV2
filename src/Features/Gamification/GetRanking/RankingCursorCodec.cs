namespace ShapeUp.Features.Gamification.GetRanking;

using System.Globalization;
using System.Text;

/// <summary>
/// Encodes keyset cursors for ranking ordered by ShapeScore desc, then UserId asc.
/// Payload format: "{shapeScore}:{userId}" (UTF-8, base64).
/// </summary>
public static class RankingCursorCodec
{
    private const char Separator = ':';

    public static string Encode(int shapeScore, int userId)
    {
        var raw = string.Create(CultureInfo.InvariantCulture, $"{shapeScore}{Separator}{userId}");
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecode(string? cursor, out int shapeScore, out int userId)
    {
        shapeScore = 0;
        userId = 0;

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var raw = Encoding.UTF8.GetString(bytes);
            var separatorIndex = raw.IndexOf(Separator);
            if (separatorIndex <= 0 || separatorIndex >= raw.Length - 1)
                return false;

            if (!int.TryParse(raw.AsSpan(0, separatorIndex), NumberStyles.Integer, CultureInfo.InvariantCulture, out shapeScore))
                return false;

            return int.TryParse(raw.AsSpan(separatorIndex + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out userId);
        }
        catch
        {
            return false;
        }
    }
}
