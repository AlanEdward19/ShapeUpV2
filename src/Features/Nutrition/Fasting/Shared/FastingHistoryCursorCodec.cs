using System.Globalization;
using System.Text;

namespace ShapeUp.Features.Nutrition.Fasting.Shared;

public static class FastingHistoryCursorCodec
{
    public static string Encode(DateTime completedAtUtc, Guid id)
    {
        var raw = $"{completedAtUtc.Ticks}:{id:D}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecode(string? cursor, out DateTime completedAtUtc, out Guid id)
    {
        completedAtUtc = default;
        id = default;

        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
            var separator = raw.IndexOf(':');
            if (separator <= 0 || separator >= raw.Length - 1)
                return false;

            if (!long.TryParse(raw[..separator], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks))
                return false;

            if (!Guid.TryParse(raw[(separator + 1)..], out id))
                return false;

            completedAtUtc = new DateTime(ticks, DateTimeKind.Utc);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
