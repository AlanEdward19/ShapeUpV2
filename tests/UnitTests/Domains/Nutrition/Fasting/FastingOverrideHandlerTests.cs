using Microsoft.AspNetCore.Http;
using ShapeUp.Features.Nutrition.Fasting.CancelOverride;
using ShapeUp.Features.Nutrition.Fasting.EndOverrideEarly;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.StartOverride;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Nutrition.Fasting;

public sealed class FastingOverrideHandlerTests
{
    private static readonly DateTime Now = new(2026, 6, 15, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Start_WithSavedAgenda_CreatesFastingOverrideWithFastEndsAtNowPlusFastHours()
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
            UpdatedAtUtc = Now
        });
        await db.SaveChangesAsync();

        var handler = new StartFastingOverrideHandler(db, new FixedUtcClock(Now));
        var result = await handler.HandleAsync(new StartFastingOverrideCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FastingOverride.StatusFasting, result.Value!.Status);
        Assert.Equal(Now.AddHours(16), result.Value.FastEndsAtUtc);
        Assert.Equal(Now.AddHours(24), result.Value.EatEndsAtUtc);
    }

    [Fact]
    public async Task Start_WithoutSavedAgenda_Returns400()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingAgendas.Add(new FastingAgenda
        {
            UserId = FastingTestSupport.UserId,
            RecommendedProtocol = "16:8",
            RecommendedFastHours = 16,
            UpdatedAtUtc = Now
        });
        await db.SaveChangesAsync();

        var handler = new StartFastingOverrideHandler(db, new FixedUtcClock(Now));
        var result = await handler.HandleAsync(new StartFastingOverrideCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Start_WhenActiveOverrideExists_Returns409AndLeavesExistingUnchanged()
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
            UpdatedAtUtc = Now
        });
        var existingId = Guid.NewGuid();
        var existingFastEnds = Now.AddHours(10);
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = existingId,
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusFasting,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = Now.AddHours(-2),
            FastEndsAtUtc = existingFastEnds,
            EatEndsAtUtc = existingFastEnds.AddHours(8)
        });
        await db.SaveChangesAsync();

        var handler = new StartFastingOverrideHandler(db, new FixedUtcClock(Now));
        var result = await handler.HandleAsync(new StartFastingOverrideCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status409Conflict, result.Error!.StatusCode);
        Assert.Single(db.FastingOverrides);
        Assert.Equal(existingFastEnds, db.FastingOverrides.Single().FastEndsAtUtc);
    }

    [Fact]
    public async Task EndEarly_WhileFasting_SetsEatingWithEatEndsAtNowPlusEatHours()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusFasting,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = Now.AddHours(-1),
            FastEndsAtUtc = Now.AddHours(15),
            EatEndsAtUtc = Now.AddHours(23)
        });
        await db.SaveChangesAsync();

        var handler = new EndFastingOverrideEarlyHandler(db, new FixedUtcClock(Now));
        var result = await handler.HandleAsync(new EndFastingOverrideEarlyCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FastingOverride.StatusEating, result.Value!.Status);
        Assert.Equal(Now.AddHours(8), result.Value.EatEndsAtUtc);
    }

    [Fact]
    public async Task EndEarly_WhileEating_Returns409()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusEating,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = Now.AddHours(-20),
            FastEndsAtUtc = Now.AddHours(-4),
            EatEndsAtUtc = Now.AddHours(4)
        });
        await db.SaveChangesAsync();

        var handler = new EndFastingOverrideEarlyHandler(db, new FixedUtcClock(Now));
        var result = await handler.HandleAsync(new EndFastingOverrideEarlyCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status409Conflict, result.Error!.StatusCode);
        Assert.Equal(Now.AddHours(4), db.FastingOverrides.Single().EatEndsAtUtc);
    }

    [Fact]
    public async Task Cancel_WhileFasting_MarksCancelledAndKeepsAgenda()
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
            UpdatedAtUtc = Now
        });
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusFasting,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = Now,
            FastEndsAtUtc = Now.AddHours(16),
            EatEndsAtUtc = Now.AddHours(24)
        });
        await db.SaveChangesAsync();

        var handler = new CancelFastingOverrideHandler(db, new FixedUtcClock(Now));
        var result = await handler.HandleAsync(new CancelFastingOverrideCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(FastingOverride.StatusCancelled, result.Value!.Status);
        Assert.Equal(16, db.FastingAgendas.Single().FastHours);
    }

    [Fact]
    public async Task Cancel_WithNoActiveOverride_Returns409()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var handler = new CancelFastingOverrideHandler(db, new FixedUtcClock(Now));

        var result = await handler.HandleAsync(new CancelFastingOverrideCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status409Conflict, result.Error!.StatusCode);
    }

    [Fact]
    public async Task EndEarly_WithNoActiveOverride_Returns409()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var handler = new EndFastingOverrideEarlyHandler(db, new FixedUtcClock(Now));

        var result = await handler.HandleAsync(new EndFastingOverrideEarlyCommand(), FastingTestSupport.UserId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status409Conflict, result.Error!.StatusCode);
    }
}
