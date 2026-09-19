using ShapeUp.Features.Nutrition.Fasting.PutAgenda;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Fasting;

public sealed class PutFastingAgendaHandlerTests
{
    [Fact]
    public async Task HandleAsync_Preset16x8_PersistsFastAndEatHours()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var clock = new FixedUtcClock(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        var handler = new PutFastingAgendaHandler(db, clock, new PutFastingAgendaCommandValidator());

        var result = await handler.HandleAsync(
            new PutFastingAgendaCommand("16:8", 720, FastingTestSupport.SaoPaulo, null),
            FastingTestSupport.UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(16, result.Value!.Agenda.FastHours);
        Assert.Equal(8, result.Value.Agenda.EatHours);
        Assert.Equal("16:8", result.Value.Agenda.Protocol);

        var stored = db.FastingAgendas.Single(a => a.UserId == FastingTestSupport.UserId);
        Assert.Equal(16, stored.FastHours);
        Assert.Equal(8, stored.EatHours);
    }

    [Fact]
    public async Task HandleAsync_CustomFastHours15_PersistsEatHours9()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var clock = new FixedUtcClock(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        var handler = new PutFastingAgendaHandler(db, clock, new PutFastingAgendaCommandValidator());

        var result = await handler.HandleAsync(
            new PutFastingAgendaCommand("custom", 720, FastingTestSupport.SaoPaulo, 15),
            FastingTestSupport.UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value!.Agenda.FastHours);
        Assert.Equal(9, result.Value.Agenda.EatHours);
        Assert.Equal("custom", result.Value.Agenda.Protocol);
    }

    [Fact]
    public async Task HandleAsync_DuringActiveOverride_DoesNotChangeOverrideTimestamps()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var fastEnds = new DateTime(2026, 6, 16, 10, 0, 0, DateTimeKind.Utc);
        var eatEnds = new DateTime(2026, 6, 16, 18, 0, 0, DateTimeKind.Utc);
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusFasting,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = new DateTime(2026, 6, 15, 18, 0, 0, DateTimeKind.Utc),
            FastEndsAtUtc = fastEnds,
            EatEndsAtUtc = eatEnds
        });
        await db.SaveChangesAsync();

        var clock = new FixedUtcClock(new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc));
        var handler = new PutFastingAgendaHandler(db, clock, new PutFastingAgendaCommandValidator());

        var result = await handler.HandleAsync(
            new PutFastingAgendaCommand("18:6", 780, FastingTestSupport.SaoPaulo, null),
            FastingTestSupport.UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(18, result.Value!.Agenda.FastHours);

        var storedOverride = db.FastingOverrides.Single();
        Assert.Equal(fastEnds, storedOverride.FastEndsAtUtc);
        Assert.Equal(eatEnds, storedOverride.EatEndsAtUtc);
        Assert.Equal(FastingOverride.StatusFasting, storedOverride.Status);
    }
}
