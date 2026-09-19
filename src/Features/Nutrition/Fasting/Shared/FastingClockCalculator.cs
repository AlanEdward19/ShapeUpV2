namespace ShapeUp.Features.Nutrition.Fasting.Shared;

public sealed class FastingClockCalculator
{
    public const string StatusIdle = "Idle";
    public const string StatusFasting = "Fasting";
    public const string StatusEating = "Eating";

    public static (int FastHours, int EatHours)? ParsePresetProtocol(string protocol) =>
        protocol switch
        {
            "14:10" => (14, 10),
            "16:8" => (16, 8),
            "18:6" => (18, 6),
            "20:4" => (20, 4),
            _ => null
        };

    public static (int FastHours, int EatHours)? ParseProtocol(string protocol, int? customFastHours)
    {
        var preset = ParsePresetProtocol(protocol);
        if (preset is not null)
            return preset;

        if (protocol == "custom" && customFastHours is >= 12 and <= 23)
            return (customFastHours.Value, 24 - customFastHours.Value);

        return null;
    }

    public FastingClockState FromAgenda(
        int fastHours,
        int eatHours,
        int eatingStartMinutes,
        string timeZoneId,
        DateTime utcNow)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc), tz);
        var eatingStart = ResolveEatingStartLocal(DateOnly.FromDateTime(localNow), eatingStartMinutes, tz);
        var eatingEnd = eatingStart.AddHours(eatHours);

        if (localNow >= eatingStart && localNow < eatingEnd)
            return new FastingClockState(StatusEating, ToUtc(eatingEnd, tz));

        var nextStart = localNow < eatingStart
            ? eatingStart
            : ResolveEatingStartLocal(DateOnly.FromDateTime(localNow).AddDays(1), eatingStartMinutes, tz);

        return new FastingClockState(StatusFasting, ToUtc(nextStart, tz));
    }

    public FastingClockState FromOverride(string status, DateTime fastEndsAtUtc, DateTime? eatEndsAtUtc, DateTime utcNow)
    {
        if (status == StatusFasting)
            return new FastingClockState(StatusFasting, DateTime.SpecifyKind(fastEndsAtUtc, DateTimeKind.Utc));

        if (status == StatusEating && eatEndsAtUtc is not null)
            return new FastingClockState(StatusEating, DateTime.SpecifyKind(eatEndsAtUtc.Value, DateTimeKind.Utc));

        return new FastingClockState(StatusIdle, null);
    }

    public static FastingClockState Idle() => new(StatusIdle, null);

    public static DateTime ResolveEatingStartLocal(DateOnly date, int eatingStartMinutes, TimeZoneInfo tz)
    {
        var local = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(eatingStartMinutes)));
        if (!tz.IsInvalidTime(local))
            return local;

        var candidate = local;
        while (tz.IsInvalidTime(candidate))
            candidate = candidate.AddMinutes(30);

        return candidate;
    }

    private static DateTime ToUtc(DateTime localUnspecified, TimeZoneInfo tz)
    {
        var unspecified = DateTime.SpecifyKind(localUnspecified, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, tz);
    }
}

public sealed record FastingClockState(string Status, DateTime? BoundaryAt);
