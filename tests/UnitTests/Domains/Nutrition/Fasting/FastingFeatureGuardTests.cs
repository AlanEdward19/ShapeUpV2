namespace UnitTests.Domains.Nutrition.Fasting;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.PlatformFeatureFlags.Infrastructure.Data;
using ShapeUp.Features.PlatformFeatureFlags.Shared.Entities;

public sealed class FastingFeatureGuardTests
{
    private static PlatformFeatureFlagsDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PlatformFeatureFlagsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PlatformFeatureFlagsDbContext(options);
    }

    [Fact]
    public async Task EnsureEnabledAsync_WhenFlagDisabled_ReturnsNutritionFastingDisabled404()
    {
        await using var context = NewContext();
        context.Flags.Add(new PlatformFeatureFlag
        {
            Key = FastingFeatureGuard.FeatureKey,
            Enabled = false,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var guard = new FastingFeatureGuard(new FeatureFlagReader(context));
        var result = await guard.EnsureEnabledAsync(CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("nutrition.fasting.disabled", result.Error!.Code);
        Assert.Equal(404, result.Error.StatusCode);
    }

    [Fact]
    public async Task EnsureEnabledAsync_WhenFlagEnabled_ReturnsSuccess()
    {
        await using var context = NewContext();
        context.Flags.Add(new PlatformFeatureFlag
        {
            Key = FastingFeatureGuard.FeatureKey,
            Enabled = true,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var guard = new FastingFeatureGuard(new FeatureFlagReader(context));
        var result = await guard.EnsureEnabledAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task EnsureEnabledAsync_WhenKeyMissing_FailOpenSuccess()
    {
        await using var context = NewContext();
        var guard = new FastingFeatureGuard(new FeatureFlagReader(context));
        var result = await guard.EnsureEnabledAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}
