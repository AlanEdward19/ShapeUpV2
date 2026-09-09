using ShapeUp.Features.Gamification.Shared.AntiCheat;
using ShapeUp.Features.Gamification.Shared.Enums;

namespace UnitTests.Domains.Gamification;

public class AntiCheatClassifierDurationTests
{
    private readonly AntiCheatClassifier _sut = new();
    private static readonly DateTime EndedAt = new(2026, 9, 9, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ClassifyAsync_WhenExercisesEmpty_ReturnsInvalid()
    {
        var session = AntiCheatClassifierTestHelpers.CreateSession("current", 1, EndedAt, 120, []);
        session.Exercises = [];

        var result = await _sut.ClassifyAsync(session, [], CancellationToken.None);

        Assert.Equal(ActivityClassification.Invalid, result.Classification);
    }

    [Theory]
    [InlineData(0.29, ActivityClassification.Invalid)]
    [InlineData(0.3, ActivityClassification.Suspicious)]
    [InlineData(0.31, ActivityClassification.Suspicious)]
    [InlineData(0.49, ActivityClassification.Suspicious)]
    [InlineData(0.5, ActivityClassification.LikelyValid)]
    [InlineData(0.51, ActivityClassification.LikelyValid)]
    [InlineData(0.79, ActivityClassification.LikelyValid)]
    [InlineData(0.8, ActivityClassification.Verified)]
    [InlineData(0.81, ActivityClassification.Verified)]
    public async Task ClassifyAsync_DurationRatioBoundary_ReturnsExpectedClassification(
        double ratio,
        ActivityClassification expected)
    {
        var session = AntiCheatClassifierTestHelpers.CreateDurationRatioSession("current", 1, EndedAt, ratio);
        var priors = AntiCheatClassifierTestHelpers.CreateVolumeBaseline(1, EndedAt, 3);

        var result = await _sut.ClassifyAsync(session, priors, CancellationToken.None);

        Assert.Equal(expected, result.Classification);
    }
}
