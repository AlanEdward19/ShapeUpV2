namespace UnitTests.Domains.Nutrition.Fasting;

using ShapeUp.Features.Nutrition.Fasting.Shared;

public sealed class FastingClockCalculatorTests
{
    private const string SaoPaulo = "America/Sao_Paulo";
    private readonly FastingClockCalculator _calculator = new();

    [Fact]
    public void FromAgenda_16x8_At11LocalSaoPaulo_IsFastingUntilNoon()
    {
        var utcNow = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var expectedBoundary = new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc);

        var clock = _calculator.FromAgenda(16, 8, 720, SaoPaulo, utcNow);

        Assert.Equal(FastingClockCalculator.StatusFasting, clock.Status);
        Assert.Equal(expectedBoundary, clock.BoundaryAt);
    }

    [Fact]
    public void FromAgenda_16x8_At12LocalSaoPaulo_IsEatingUntil20Local()
    {
        var utcNow = new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc);
        var expectedBoundary = new DateTime(2026, 6, 15, 23, 0, 0, DateTimeKind.Utc);

        var clock = _calculator.FromAgenda(16, 8, 720, SaoPaulo, utcNow);

        Assert.Equal(FastingClockCalculator.StatusEating, clock.Status);
        Assert.Equal(expectedBoundary, clock.BoundaryAt);
    }

    [Fact]
    public void FromAgenda_16x8_EatingStartMidnight_EatingWindow00To08()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(SaoPaulo);
        var duringEatUtc = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 6, 15, 7, 0, 0, DateTimeKind.Unspecified), tz);
        var duringFastUtc = TimeZoneInfo.ConvertTimeToUtc(
            new DateTime(2026, 6, 15, 9, 0, 0, DateTimeKind.Unspecified), tz);

        var eating = _calculator.FromAgenda(16, 8, 0, SaoPaulo, duringEatUtc);
        var fasting = _calculator.FromAgenda(16, 8, 0, SaoPaulo, duringFastUtc);

        Assert.Equal(FastingClockCalculator.StatusEating, eating.Status);
        Assert.Equal(
            TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 6, 15, 8, 0, 0, DateTimeKind.Unspecified), tz),
            eating.BoundaryAt);

        Assert.Equal(FastingClockCalculator.StatusFasting, fasting.Status);
        Assert.Equal(
            TimeZoneInfo.ConvertTimeToUtc(new DateTime(2026, 6, 16, 0, 0, 0, DateTimeKind.Unspecified), tz),
            fasting.BoundaryAt);
    }

    [Fact]
    public void ResolveEatingStartLocal_WhenDstSkipsHour_UsesNextValidLocalTime()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
        var springForwardSunday = new DateOnly(2026, 3, 8);
        const int twoAmMinutes = 120;

        var resolved = FastingClockCalculator.ResolveEatingStartLocal(springForwardSunday, twoAmMinutes, tz);

        Assert.False(tz.IsInvalidTime(resolved));
        Assert.Equal(new DateTime(2026, 3, 8, 3, 0, 0), resolved);
    }

    [Fact]
    public void FromOverride_Fasting_UsesFastEndsAtUtc()
    {
        var fastEnds = new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc);
        var clock = _calculator.FromOverride(FastingClockCalculator.StatusFasting, fastEnds, null, DateTime.UtcNow);

        Assert.Equal(FastingClockCalculator.StatusFasting, clock.Status);
        Assert.Equal(fastEnds, clock.BoundaryAt);
    }

    [Fact]
    public void FromOverride_Eating_UsesEatEndsAtUtc()
    {
        var eatEnds = new DateTime(2026, 6, 15, 22, 0, 0, DateTimeKind.Utc);
        var clock = _calculator.FromOverride(
            FastingClockCalculator.StatusEating,
            DateTime.UtcNow,
            eatEnds,
            DateTime.UtcNow);

        Assert.Equal(FastingClockCalculator.StatusEating, clock.Status);
        Assert.Equal(eatEnds, clock.BoundaryAt);
    }

    [Fact]
    public void ParsePresetProtocol_KnownPresets_ReturnHours()
    {
        Assert.Equal((16, 8), FastingClockCalculator.ParsePresetProtocol("16:8"));
        Assert.Equal((14, 10), FastingClockCalculator.ParsePresetProtocol("14:10"));
        Assert.Null(FastingClockCalculator.ParsePresetProtocol("99:99"));
    }
}
