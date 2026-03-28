namespace UnitTests.Domains.Training.Dashboard;

using ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;

public class TrainingDashboardHandlerTests
{
    private readonly Mock<IWorkoutSessionRepository> _workoutRepository = new();

    [Fact]
    public async Task GetTrainingDashboardHandler_WhenTargetPerWeekIsInvalid_ReturnsValidationFailure()
    {
        var handler = new GetTrainingDashboardHandler(_workoutRepository.Object);

        var result = await handler.HandleAsync(new GetTrainingDashboardQuery(10, 0), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Fact]
    public async Task GetTrainingDashboardHandler_WhenPreviousWeekHasNoVolume_ReturnsHundredPercentProgress()
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-((7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7));
        _workoutRepository.SetupSequence(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(
            [
                new WorkoutSessionDocument
                {
                    TargetUserId = 10,
                    ExecutedByUserId = 10,
                    IsCompleted = true,
                    StartedAtUtc = weekStart.AddDays(1),
                    Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench", Sets = [new ExecutedSetDocumentValueObject { Repetitions = 10, Load = 10, LoadUnit = "kg", SetType = "working", Rpe = 8, RestSeconds = 60 }] }]
                }
            ])
            .ReturnsAsync([])
            .ReturnsAsync([]);

        var handler = new GetTrainingDashboardHandler(_workoutRepository.Object);
        var result = await handler.HandleAsync(new GetTrainingDashboardQuery(10, 4), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value!.WeeklyVolumeProgressPercent);
    }

    [Fact]
    public async Task GetTrainingDashboardHandler_WhenHistoryHasGap_StopsStreakAtGap()
    {
        var now = DateTime.UtcNow;
        var sessions = new[]
        {
            CreateSession(now.Date),
            CreateSession(now.Date.AddDays(-1)),
            CreateSession(now.Date.AddDays(-3))
        };

        _workoutRepository.SetupSequence(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([])
            .ReturnsAsync([])
            .ReturnsAsync(sessions);

        var handler = new GetTrainingDashboardHandler(_workoutRepository.Object);
        var result = await handler.HandleAsync(new GetTrainingDashboardQuery(10, 3), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.ConsecutiveTrainingDays);
    }

    private static WorkoutSessionDocument CreateSession(DateTime day) => new()
    {
        Id = Guid.NewGuid().ToString("N").PadLeft(24, '0')[..24],
        TargetUserId = 10,
        ExecutedByUserId = 10,
        IsCompleted = true,
        StartedAtUtc = DateTime.SpecifyKind(day, DateTimeKind.Utc),
        Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench", Sets = [new ExecutedSetDocumentValueObject { Repetitions = 5, Load = 100, LoadUnit = "kg", SetType = "working", Rpe = 8, RestSeconds = 120 }] }]
    };
}


