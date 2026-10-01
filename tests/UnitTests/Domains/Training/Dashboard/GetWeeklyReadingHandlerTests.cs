namespace UnitTests.Domains.Training.Dashboard;

using ShapeUp.Features.Entitlements.Shared.Abstractions;
using ShapeUp.Features.Training.Dashboard;
using ShapeUp.Features.Training.Dashboard.GetWeeklyReading;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class GetWeeklyReadingHandlerTests
{
    private static readonly DateTime WeekStart = DateTime.UtcNow.Date.AddDays(-((7 + (DateTime.UtcNow.DayOfWeek - DayOfWeek.Monday)) % 7));

    private readonly Mock<IWorkoutSessionRepository> _workoutRepository = new();
    private readonly Mock<IEntitlementRepository> _entitlementRepository = new();

    private GetWeeklyReadingHandler CreateHandler(bool unlocked)
    {
        _entitlementRepository
            .Setup(x => x.GetEntitlementAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Entitlement(10, unlocked ? "Progresso" : "Free", unlocked ? new HashSet<string> { "weeklyProgress" } : new HashSet<string>()));
        return new GetWeeklyReadingHandler(_workoutRepository.Object, _entitlementRepository.Object);
    }

    private void SetupWeeks(IReadOnlyList<WorkoutSessionDocument> thisWeek, IReadOnlyList<WorkoutSessionDocument> previousWeek) =>
        _workoutRepository
            .SetupSequence(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(thisWeek)
            .ReturnsAsync(previousWeek);

    private static WorkoutSessionDocument Session(DateTime day, int exerciseId, string name, decimal? load, int? repetitions) =>
        new()
        {
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = true,
                        StartedAtUtc = day.AddHours(8),
            EndedAtUtc = day.AddHours(9),
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = exerciseId, ExerciseName = name, Sets = [new ExecutedSetDocumentValueObject { Load = load, Repetitions = repetitions, IsPerformed = true }] }]
        };

    [Fact]
    public async Task HandleAsync_WithoutProgressPlan_ReturnsForbiddenWithoutReadingSessions()
    {
        var handler = CreateHandler(unlocked: false);

        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(10), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
        _workoutRepository.Verify(x => x.GetCompletedByUserInRangeAsync(It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithThreeDaysOfLoggedSets_ReturnsThreeDaysAndLoadTrend()
    {
        var handler = CreateHandler(unlocked: true);
        SetupWeeks(
            [
                Session(WeekStart, 1, "Bench", 80m, 5),
                Session(WeekStart.AddDays(1), 1, "Bench", 85m, 5),
                Session(WeekStart.AddDays(1), 2, "Squat", 100m, 5),
                Session(WeekStart.AddDays(2), 3, "Row", 60m, 8)
            ],
            [
                Session(WeekStart.AddDays(-5), 1, "Bench", 80m, 5),
                Session(WeekStart.AddDays(-4), 2, "Squat", 110m, 5),
                Session(WeekStart.AddDays(-3), 4, "Curl", 20m, 10)
            ]);

        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.DaysWithWork);
        var byName = result.Value.LoadTrends.ToDictionary(t => t.ExerciseName);
        Assert.Equal(LoadTrend.Up, byName["Bench"].Trend);
        Assert.Equal(85m, byName["Bench"].CurrentMaxLoad);
        Assert.Equal(80m, byName["Bench"].PreviousMaxLoad);
        Assert.Equal(LoadTrend.Down, byName["Squat"].Trend);
        Assert.Equal(LoadTrend.NoPrevious, byName["Row"].Trend);
        Assert.DoesNotContain("Curl", byName.Keys);
    }

    [Fact]
    public async Task HandleAsync_WithSameLoadAsPreviousWeek_ReturnsSame()
    {
        var handler = CreateHandler(unlocked: true);
        SetupWeeks(
            [Session(WeekStart, 1, "Bench", 80m, 5)],
            [Session(WeekStart.AddDays(-3), 1, "Bench", 80m, 8)]);

        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(10), CancellationToken.None);

        Assert.Equal(LoadTrend.Same, Assert.Single(result.Value!.LoadTrends).Trend);
    }

    [Fact]
    public async Task HandleAsync_WhenDayHasOnlyAnOpenedSessionWithoutSets_DoesNotCountTheDay()
    {
        var handler = CreateHandler(unlocked: true);
        SetupWeeks(
            [
                Session(WeekStart, 1, "Bench", 80m, 5),
                Session(WeekStart.AddDays(1), 1, "Bench", null, null),
                new WorkoutSessionDocument { TargetUserId = 10, ExecutedByUserId = 10, IsCompleted = true, StartedAtUtc = WeekStart.AddDays(2).AddHours(8) }
            ],
            []);

        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(10), CancellationToken.None);

        Assert.Equal(1, result.Value!.DaysWithWork);
        Assert.Single(result.Value.LoadTrends);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionOnlyHasSetsPrefilledFromThePlan_DoesNotCountTheDayNorTheLoad()
    {
        var handler = CreateHandler(unlocked: true);
        var prefilled = Session(WeekStart, 1, "Bench", 80m, 5);
        prefilled.Exercises[0].Sets[0].IsPerformed = false;
        SetupWeeks([prefilled], []);

        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(10), CancellationToken.None);

        Assert.Equal(0, result.Value!.DaysWithWork);
        Assert.Empty(result.Value.LoadTrends);
    }

    [Fact]
    public async Task HandleAsync_WhenOnlyOneExerciseWasPerformed_ExcludesPrefilledExercisesAndLoads()
    {
        var handler = CreateHandler(unlocked: true);
        var session = Session(WeekStart, 1, "Bench", 80m, 5);
        session.Exercises[0].Sets.Add(new ExecutedSetDocumentValueObject { Load = 120m, Repetitions = 5, IsPerformed = false });
        session.Exercises.Add(new ExecutedExerciseDocumentValueObject
        {
            ExerciseId = 2,
            ExerciseName = "Squat",
            Sets = [new ExecutedSetDocumentValueObject { Load = 100m, Repetitions = 5, IsPerformed = false }]
        });
        SetupWeeks([session], []);

        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(10), CancellationToken.None);

        var trend = Assert.Single(result.Value!.LoadTrends);
        Assert.Equal("Bench", trend.ExerciseName);
        Assert.Equal(80m, trend.CurrentMaxLoad);
    }

    [Fact]
    public async Task HandleAsync_WhenTwoSessionsOnSameDay_CountsOneDay()
    {
        var handler = CreateHandler(unlocked: true);
        SetupWeeks([Session(WeekStart, 1, "Bench", 80m, 5), Session(WeekStart, 2, "Squat", 100m, 5)], []);

        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(10), CancellationToken.None);

        Assert.Equal(1, result.Value!.DaysWithWork);
    }
}
