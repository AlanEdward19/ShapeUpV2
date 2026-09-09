using ShapeUp.Features.Gamification.Shared.AntiCheat;
using ShapeUp.Features.Gamification.Shared.Enums;

namespace UnitTests.Domains.Gamification;

public class AntiCheatClassifierVolumeTests
{
    private readonly AntiCheatClassifier _sut = new();
    private static readonly DateTime EndedAt = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ClassifyAsync_WhenFewerThanThreePriorSessions_ReturnsLikelyValid()
    {
        var session = AntiCheatClassifierTestHelpers.CreateUniformVolumeSession("current", 1, EndedAt, 120, 100m);
        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 2);

        var result = await _sut.ClassifyAsync(session, priors, CancellationToken.None);

        Assert.Equal(ActivityClassification.LikelyValid, result.Classification);
    }

    [Theory]
    [InlineData(300, ActivityClassification.Verified)]
    [InlineData(301, ActivityClassification.Suspicious)]
    [InlineData(600, ActivityClassification.Suspicious)]
    [InlineData(601, ActivityClassification.Invalid)]
    public async Task ClassifyAsync_VolumeMultiplierBoundary_ReturnsExpectedClassification(
        decimal targetVolume,
        ActivityClassification expected)
    {
        var session = AntiCheatClassifierTestHelpers.CreateUniformVolumeSession("current", 1, EndedAt, 120, targetVolume);
        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 3, 100m);

        var result = await _sut.ClassifyAsync(session, priors, CancellationToken.None);

        Assert.Equal(expected, result.Classification);
    }
}
