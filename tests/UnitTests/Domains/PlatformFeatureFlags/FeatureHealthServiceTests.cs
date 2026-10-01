namespace UnitTests.Domains.PlatformFeatureFlags;

using ShapeUp.Features.PlatformFeatureFlags.Health;
using ShapeUp.Features.PlatformFeatureFlags.Shared.Abstractions;

public class FeatureHealthServiceTests
{
    private static readonly string[] AllKeys = ["nutrition", "training", "gamification", "gym-management", "notifications", "fasting"];

    private readonly Mock<IFeatureFlagReader> _flags = new();
    private readonly Mock<IDependencyHealthChecker> _dependencies = new();

    private FeatureHealthService CreateService(bool sql = true, bool mongo = true, bool rabbit = true)
    {
        _flags.Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _dependencies
            .Setup(x => x.CheckAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, bool>
            {
                [HealthDependencies.SqlServer] = sql,
                [HealthDependencies.Mongo] = mongo,
                [HealthDependencies.RabbitMq] = rabbit
            });
        return new FeatureHealthService(_flags.Object, _dependencies.Object);
    }

    [Fact]
    public async Task GetAsync_WhenEverythingIsUp_ReturnsAllFeaturesHealthy()
    {
        var result = await CreateService().GetAsync(CancellationToken.None);

        Assert.Equal("healthy", result.Status);
        Assert.Equal(AllKeys.OrderBy(k => k), result.Features.Keys.OrderBy(k => k));
        Assert.All(result.Features.Values, v => Assert.Equal("healthy", v));
    }

    [Fact]
    public async Task GetAsync_WhenMongoIsDown_MarksOnlyMongoDependentFeaturesUnhealthy()
    {
        var result = await CreateService(mongo: false).GetAsync(CancellationToken.None);

        Assert.Equal("unhealthy", result.Features["nutrition"]);
        Assert.Equal("unhealthy", result.Features["training"]);
        Assert.Equal("unhealthy", result.Features["fasting"]);
        Assert.Equal("healthy", result.Features["gamification"]);
        Assert.Equal("healthy", result.Features["gym-management"]);
        Assert.Equal("healthy", result.Features["notifications"]);
        Assert.Equal("healthy", result.Status);
    }

    [Fact]
    public async Task GetAsync_WhenFlagIsOff_ReportsDisabledEvenIfDependencyIsDown()
    {
        var service = CreateService(mongo: false);
        _flags.Setup(x => x.IsEnabledAsync("features.gamification", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _flags.Setup(x => x.IsEnabledAsync("features.training", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal("disabled", result.Features["gamification"]);
        Assert.Equal("disabled", result.Features["training"]);
        Assert.Equal("unhealthy", result.Features["nutrition"]);
    }

    [Fact]
    public async Task GetAsync_WhenFastingFlagIsOff_ReportsFastingDisabled()
    {
        var service = CreateService();
        _flags.Setup(x => x.IsEnabledAsync("nutrition.intermittent-fasting", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal("disabled", result.Features["fasting"]);
        Assert.Equal("healthy", result.Features["nutrition"]);
        Assert.Equal("healthy", result.Status);
    }

    [Fact]
    public async Task GetAsync_WhenFlagStoreHangs_FailsOpenWithinTheBudget()
    {
        var service = CreateService();
        _flags
            .Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>(async (_, ct) =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                return true;
            });

        var started = DateTime.UtcNow;
        var result = await service.GetAsync(CancellationToken.None);

        Assert.True(DateTime.UtcNow - started < TimeSpan.FromSeconds(10));
        Assert.All(result.Features.Values, v => Assert.Equal("healthy", v));
    }

    [Fact]
    public async Task GetAsync_WhenFlagStoreThrows_FailsOpenWithoutThrowing()
    {
        var service = CreateService(sql: false);
        _flags.Setup(x => x.IsEnabledAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Server=secret-host;Password=x"));

        var result = await service.GetAsync(CancellationToken.None);

        Assert.Equal("unhealthy", result.Features["gym-management"]);
        Assert.Equal("healthy", result.Features["training"]);
        Assert.DoesNotContain(result.Features.Values, v => v.Contains("secret-host"));
    }
}
