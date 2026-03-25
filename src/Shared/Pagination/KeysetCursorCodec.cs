namespace ShapeUp.Shared.Pagination;

using System.Globalization;
using System.Text;

public static class KeysetCursorCodec
{
    public static string EncodeLong(long value)
    {
        var raw = value.ToString(CultureInfo.InvariantCulture);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    public static bool TryDecodeLong(string? cursor, out long value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(cursor))
            return false;

        try
        {
            var bytes = Convert.FromBase64String(cursor);
            var raw = Encoding.UTF8.GetString(bytes);
            return long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
        catch
        {
            return false;
        }
    }
}

