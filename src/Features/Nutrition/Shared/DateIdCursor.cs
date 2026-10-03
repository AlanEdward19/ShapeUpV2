using System.Globalization;
using System.Text;

namespace ShapeUp.Features.Nutrition.Shared;

/// <summary>Opaque keyset cursor for lists ordered by (Date, Id): base64 of "dayNumber:id".</summary>
public static class DateIdCursor
{
    public static string Encode(DateOnly date, int id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{date.DayNumber.ToString(CultureInfo.InvariantCulture)}:{id.ToString(CultureInfo.InvariantCulture)}"));

    public static bool TryDecode(string? cursor, out DateOnly date, out int id)
    {
        date = default;
        id = 0;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var parts = Encoding.UTF8.GetString(Convert.FromBase64String(cursor)).Split(':');
            if (parts.Length != 2
                || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var dayNumber)
                || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out id)
                || dayNumber is < 0 or > 3652058)
                return false;

            date = DateOnly.FromDayNumber(dayNumber);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
