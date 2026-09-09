using ShapeUp.Features.Gamification.Shared.AntiCheat;
using ShapeUp.Features.Gamification.Shared.Enums;

namespace UnitTests.Domains.Gamification;

public class AntiCheatClassifierAggregationTests
{
    private readonly AntiCheatClassifier _sut = new();
    private static readonly DateTime EndedAt = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ClassifyAsync_WhenInvalidDuplicationAndVerifiedDuration_WorstWinsInvalid()
    {
        var prior = AntiCheatClassifierTestHelpers.CreateSession(
            "prior",
            1,
            EndedAt.AddMinutes(-2),
            120,
            [(1, 10, 20m, 90)]);

        var current = AntiCheatClassifierTestHelpers.CreateDurationRatioSession("current", 1, EndedAt, 0.9);
        current.Exercises = prior.Exercises;

        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 3);
        priors = priors.Append(prior).ToList();

        var result = await _sut.ClassifyAsync(current, priors, CancellationToken.None);

        Assert.Equal(ActivityClassification.Invalid, result.Classification);
    }

    [Fact]
    public async Task ClassifyAsync_WhenSuspiciousDurationAndLikelyValidVolume_WorstWinsSuspicious()
    {
        var session = AntiCheatClassifierTestHelpers.CreateDurationRatioSession("current", 1, EndedAt, 0.4);
        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 2);

        var result = await _sut.ClassifyAsync(session, priors, CancellationToken.None);

        Assert.Equal(ActivityClassification.Suspicious, result.Classification);
    }

    [Fact]
    public async Task ClassifyAsync_WhenLikelyValidDurationAndVerifiedVolume_WorstWinsLikelyValid()
    {
        var session = AntiCheatClassifierTestHelpers.CreateDurationRatioSession("current", 1, EndedAt, 0.6);
        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 3, 100m);

        var result = await _sut.ClassifyAsync(session, priors, CancellationToken.None);

        Assert.Equal(ActivityClassification.LikelyValid, result.Classification);
    }

    [Fact]
    public async Task ClassifyAsync_WhenInvalidVolumeAndVerifiedDuplication_WorstWinsInvalid()
    {
        var session = AntiCheatClassifierTestHelpers.CreateUniformVolumeSession("current", 1, EndedAt, 120, 601m);
        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 3, 100m);

        var result = await _sut.ClassifyAsync(session, priors, CancellationToken.None);

        Assert.Equal(ActivityClassification.Invalid, result.Classification);
    }
}
