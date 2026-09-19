namespace UnitTests.Domains.Nutrition.Fasting;

using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Shared.Entities;
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
    public async Task EnsureEnabledAsync_WhenFlagDisabled_DoesNotRemoveStoredFastingAgenda()
    {
        await using var nutritionDb = FastingTestSupport.CreateDbContext();
        nutritionDb.FastingAgendas.Add(new FastingAgenda
        {
            UserId = FastingTestSupport.UserId,
            Protocol = "16:8",
            FastHours = 16,
            EatHours = 8,
            EatingStartMinutes = 720,
            TimeZone = FastingTestSupport.SaoPaulo,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await nutritionDb.SaveChangesAsync();

        await using var flagsDb = NewContext();
        flagsDb.Flags.Add(new PlatformFeatureFlag
        {
            Key = FastingFeatureGuard.FeatureKey,
            Enabled = false,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await flagsDb.SaveChangesAsync();

        var guard = new FastingFeatureGuard(new FeatureFlagReader(flagsDb));
        var guardResult = await guard.EnsureEnabledAsync(CancellationToken.None);

        Assert.True(guardResult.IsFailure);

        var storedAgenda = await nutritionDb.FastingAgendas
            .FirstOrDefaultAsync(a => a.UserId == FastingTestSupport.UserId);
        Assert.NotNull(storedAgenda);
        Assert.Equal("16:8", storedAgenda!.Protocol);
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
