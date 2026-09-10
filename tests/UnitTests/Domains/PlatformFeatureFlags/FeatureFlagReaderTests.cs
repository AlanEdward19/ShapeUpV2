namespace UnitTests.Domains.PlatformFeatureFlags;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data;
using ShapeUp.Features.PlatformFeatureFlags.Shared.Entities;

public sealed class FeatureFlagReaderTests
{
    private static PlatformFeatureFlagsDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PlatformFeatureFlagsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PlatformFeatureFlagsDbContext(options);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenKeyMissing_ReturnsTrue()
    {
        await using var context = NewContext();
        var reader = new FeatureFlagReader(context);

        var enabled = await reader.IsEnabledAsync("notifications.email-enabled", CancellationToken.None);

        Assert.True(enabled);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFlagDisabled_ReturnsFalse()
    {
        await using var context = NewContext();
        context.Flags.Add(new PlatformFeatureFlag
        {
            Key = "notifications.email-enabled",
            Enabled = false,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var reader = new FeatureFlagReader(context);
        var enabled = await reader.IsEnabledAsync("notifications.email-enabled", CancellationToken.None);

        Assert.False(enabled);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenFlagEnabled_ReturnsTrue()
    {
        await using var context = NewContext();
        context.Flags.Add(new PlatformFeatureFlag
        {
            Key = "notifications.email-enabled",
            Enabled = true,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var reader = new FeatureFlagReader(context);
        var enabled = await reader.IsEnabledAsync("notifications.email-enabled", CancellationToken.None);

        Assert.True(enabled);
    }

    [Fact]
    public async Task IsEnabledAsync_WhenOtherKeyMissing_StillFailOpen()
    {
        await using var context = NewContext();
        var reader = new FeatureFlagReader(context);

        var enabled = await reader.IsEnabledAsync("feature.unknown", CancellationToken.None);

        Assert.True(enabled);
    }
}
