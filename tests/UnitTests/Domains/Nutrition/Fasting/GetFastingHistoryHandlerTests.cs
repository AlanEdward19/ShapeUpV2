using ShapeUp.Features.Nutrition.Fasting.GetHistory;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Fasting;

public sealed class GetFastingHistoryHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenNoHistory_ReturnsEmptyItems()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var handler = new GetFastingHistoryHandler(db, new GetFastingHistoryQueryValidator());

        var result = await handler.HandleAsync(
            new GetFastingHistoryQuery(null, null),
            FastingTestSupport.UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value!.Items);
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAtMost14NewestCompletedOrCancelledFirst()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var baseTime = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < 16; i++)
        {
            db.FastingOverrides.Add(new FastingOverride
            {
                Id = Guid.NewGuid(),
                UserId = FastingTestSupport.UserId,
                Status = i % 2 == 0 ? FastingOverride.StatusCompleted : FastingOverride.StatusCancelled,
                Protocol = "16:8",
                FastHours = 16,
                EatHours = 8,
                StartedAtUtc = baseTime.AddDays(-i),
                FastEndsAtUtc = baseTime.AddDays(-i).AddHours(16),
                CompletedAtUtc = baseTime.AddDays(-i).AddHours(20)
            });
        }

        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusFasting,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = baseTime,
            FastEndsAtUtc = baseTime.AddHours(16)
        });

        await db.SaveChangesAsync();

        var handler = new GetFastingHistoryHandler(db, new GetFastingHistoryQueryValidator());
        var result = await handler.HandleAsync(
            new GetFastingHistoryQuery(null, null),
            FastingTestSupport.UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(14, result.Value!.Items.Length);
        Assert.NotNull(result.Value.NextCursor);

        for (var i = 1; i < result.Value.Items.Length; i++)
            Assert.True(result.Value.Items[i - 1].StartedAtUtc >= result.Value.Items[i].StartedAtUtc);
    }

    [Fact]
    public async Task HandleAsync_CancelledDuringFast_ReportsDurationToCancelTime()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var started = new DateTime(2026, 6, 10, 8, 0, 0, DateTimeKind.Utc);
        var cancelled = started.AddHours(5);
        db.FastingOverrides.Add(new FastingOverride
        {
            Id = Guid.NewGuid(),
            UserId = FastingTestSupport.UserId,
            Status = FastingOverride.StatusCancelled,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            StartedAtUtc = started,
            FastEndsAtUtc = started.AddHours(16),
            CompletedAtUtc = cancelled
        });
        await db.SaveChangesAsync();

        var handler = new GetFastingHistoryHandler(db, new GetFastingHistoryQueryValidator());
        var result = await handler.HandleAsync(
            new GetFastingHistoryQuery(null, null),
            FastingTestSupport.UserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5 * 3600, result.Value!.Items[0].FastingDurationSeconds);
    }
}
