using ShapeUp.Features.Gamification.Shared.AntiCheat;
using ShapeUp.Features.Gamification.Shared.Enums;

namespace UnitTests.Domains.Gamification;

public class AntiCheatClassifierDuplicationTests
{
    private readonly AntiCheatClassifier _sut = new();
    private static readonly DateTime EndedAt = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ClassifyAsync_WhenExactReplayWithinFiveMinutes_ReturnsInvalid()
    {
        var prior = AntiCheatClassifierTestHelpers.CreateSession(
            "prior",
            1,
            EndedAt.AddMinutes(-2),
            120,
            [(1, 10, 20m, 90), (2, 8, 40m, 120)]);

        var current = AntiCheatClassifierTestHelpers.CreateSession(
            "current",
            1,
            EndedAt,
            120,
            [(1, 10, 20m, 90), (2, 8, 40m, 120)]);

        var result = await _sut.ClassifyAsync(current, [prior], CancellationToken.None);

        Assert.Equal(ActivityClassification.Invalid, result.Classification);
    }

    [Fact]
    public async Task ClassifyAsync_WhenPartialMatchIsExactlyEightyPercent_ReturnsSuspicious()
    {
        var prior = AntiCheatClassifierTestHelpers.CreateSession(
            "prior",
            1,
            EndedAt.AddMinutes(-1),
            500,
            [
                (1, 10, 10m, 90),
                (2, 10, 10m, 90),
                (3, 10, 10m, 90),
                (4, 10, 10m, 90),
                (5, 10, 99m, 90)
            ]);

        var current = AntiCheatClassifierTestHelpers.CreateSession(
            "current",
            1,
            EndedAt,
            500,
            [
                (1, 10, 10m, 90),
                (2, 10, 10m, 90),
                (3, 10, 10m, 90),
                (4, 10, 10m, 90),
                (5, 10, 10m, 90)
            ]);

        var result = await _sut.ClassifyAsync(current, [prior], CancellationToken.None);

        Assert.Equal(ActivityClassification.Suspicious, result.Classification);
    }

    [Fact]
    public async Task ClassifyAsync_WhenPartialMatchIsBelowEightyPercent_ReturnsVerified()
    {
        var recentPrior = AntiCheatClassifierTestHelpers.CreateSession(
            "recent-prior",
            1,
            EndedAt.AddMinutes(-1),
            500,
            [
                (1, 10, 10m, 90),
                (2, 10, 10m, 90),
                (3, 10, 10m, 90),
                (4, 10, 99m, 90),
                (5, 10, 99m, 90)
            ]);

        var current = AntiCheatClassifierTestHelpers.CreateSession(
            "current",
            1,
            EndedAt,
            500,
            [
                (1, 10, 10m, 90),
                (2, 10, 10m, 90),
                (3, 10, 10m, 90),
                (4, 10, 10m, 90),
                (5, 10, 10m, 90)
            ]);

        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 3, 500m);
        priors = priors.Append(recentPrior).ToList();

        var result = await _sut.ClassifyAsync(current, priors, CancellationToken.None);

        Assert.Equal(ActivityClassification.Verified, result.Classification);
    }

    [Fact]
    public async Task ClassifyAsync_WhenNoSimilarPriorSession_ReturnsVerified()
    {
        var unrelatedPrior = AntiCheatClassifierTestHelpers.CreateSession(
            "unrelated-prior",
            1,
            EndedAt.AddDays(-1),
            120,
            [(1, 10, 10m, 90)]);

        var current = AntiCheatClassifierTestHelpers.CreateUniformVolumeSession("current", 1, EndedAt, 120, 100m);
        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 3, 100m);
        priors = priors.Append(unrelatedPrior).ToList();

        var result = await _sut.ClassifyAsync(current, priors, CancellationToken.None);

        Assert.Equal(ActivityClassification.Verified, result.Classification);
    }
}
