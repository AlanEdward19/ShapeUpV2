using ShapeUp.Features.Nutrition.Fasting.GetClock;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Fasting;

public sealed class GetFastingClockHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoAgendaAndNoOverride_ReturnsIdleWithNulls()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var handler = CreateHandler(db, new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc));

        var result = await handler.HandleAsync(new GetFastingClockQuery(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Agenda);
        Assert.Null(result.Value.Override);
        Assert.Equal(FastingClockCalculator.StatusIdle, result.Value.Clock.Status);
        Assert.Null(result.Value.Clock.BoundaryAt);
        Assert.Null(result.Value.Clock.Source);
    }

    [Fact]
    public async Task HandleAsync_WithAgendaOnly_At11LocalSaoPaulo_ReturnsFastingFromAgenda()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(new FastingAgenda
        {
            UserId = FastingTestSupport.UserId,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            EatingStartMinutes = 720,
            TimeZone = FastingTestSupport.SaoPaulo,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var utcNow = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var handler = CreateHandler(db, utcNow);

        var result = await handler.HandleAsync(new GetFastingClockQuery(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FastingClockCalculator.StatusFasting, result.Value!.Clock.Status);
        Assert.Equal(FastingMapper.ClockSourceAgenda, result.Value.Clock.Source);
        Assert.Equal(new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc), result.Value.Clock.BoundaryAt);
    }

    [Fact]
    public async Task HandleAsync_WithActiveOverride_ReturnsOverrideSourceAndUtcTimestamps()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var fastEnds = new DateTime(2026, 6, 16, 6, 0, 0, DateTimeKind.Utc);
        var eatEnds = new DateTime(2026, 6, 16, 14, 0, 0, DateTimeKind.Utc);
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusFasting,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc),
            FastEndsAtUtc = fastEnds,
            EatEndsAtUtc = eatEnds
        });
        await db.SaveChangesAsync();

        var handler = CreateHandler(db, new DateTime(2026, 6, 15, 15, 0, 0, DateTimeKind.Utc));

        var result = await handler.HandleAsync(new GetFastingClockQuery(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Override);
        Assert.Equal(fastEnds, result.Value.Override!.FastEndsAtUtc);
        Assert.Equal(FastingMapper.ClockSourceOverride, result.Value.Clock.Source);
        Assert.Equal(FastingClockCalculator.StatusFasting, result.Value.Clock.Status);
        Assert.Equal(fastEnds, result.Value.Clock.BoundaryAt);
    }

    [Fact]
    public async Task HandleAsync_WhenEatEndsAtIsPast_LazyCompletesOverrideAndReturnsAgendaClock()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(new FastingAgenda
        {
            UserId = FastingTestSupport.UserId,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            EatingStartMinutes = 720,
            TimeZone = FastingTestSupport.SaoPaulo,
            UpdatedAtUtc = DateTime.UtcNow
        });
        var overrideId = Guid.NewGuid();
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = overrideId,
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusEating,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc),
            FastEndsAtUtc = new DateTime(2026, 6, 15, 4, 0, 0, DateTimeKind.Utc),
            EatEndsAtUtc = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc)
        });
        await db.SaveChangesAsync();

        var utcNow = new DateTime(2026, 6, 15, 14, 0, 0, DateTimeKind.Utc);
        var handler = CreateHandler(db, utcNow);

        var result = await handler.HandleAsync(new GetFastingClockQuery(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.Override);
        Assert.Equal(FastingMapper.ClockSourceAgenda, result.Value.Clock.Source);
        Assert.Equal(FastingClockCalculator.StatusFasting, result.Value.Clock.Status);

        var stored = db.FastingOverrides.Single(o => o.Id == overrideId);
        Assert.Equal(FastingOverride.StatusCompleted, stored.Status);
        Assert.Equal(utcNow, stored.CompletedAtUtc);
    }

    private static GetFastingClockHandler CreateHandler(NutritionDbContext db, DateTime utcNow) =>
        new(db, new FixedUtcClock(utcNow), new FastingClockCalculator());
}
