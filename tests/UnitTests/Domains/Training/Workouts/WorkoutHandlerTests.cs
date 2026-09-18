namespace UnitTests.Domains.Training.Workouts;

using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.GetWorkoutSessionsByUser;

public class WorkoutHandlerTests
{
    private readonly Mock<IWorkoutSessionRepository> _workoutRepository = new();
    private readonly Mock<ITrainingAccessPolicy> _accessPolicy = new();

    [Fact]
    public async Task CompleteWorkoutSessionHandler_WhenActorHasNoAccess_ReturnsForbidden()
    {
        _workoutRepository.Setup(x => x.GetByIdAsync("session-1", default)).ReturnsAsync(new WorkoutSessionDocument
        {
            Id = "session-1",
            TargetUserId = 20,
            ExecutedByUserId = 20,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1)
        });

        var handler = new CompleteWorkoutSessionHandler(_workoutRepository.Object, new CompleteWorkoutSessionCommandValidator());
        var result = await handler.HandleAsync(new CompleteWorkoutSessionCommand("session-1", DateTime.UtcNow, 8), 99, default);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task CompleteWorkoutSessionHandler_WhenAlreadyCompleted_ReturnsConflict()
    {
        _workoutRepository.Setup(x => x.GetByIdAsync("session-1", default)).ReturnsAsync(new WorkoutSessionDocument
        {
            Id = "session-1",
            TargetUserId = 20,
            ExecutedByUserId = 20,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            IsCompleted = true
        });

        var handler = new CompleteWorkoutSessionHandler(_workoutRepository.Object, new CompleteWorkoutSessionCommandValidator());
        var result = await handler.HandleAsync(new CompleteWorkoutSessionCommand("session-1", DateTime.UtcNow, 8), 20, default);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
    }

    [Fact]
    public async Task CompleteWorkoutSessionHandler_WhenPrBeaten_UpdatesRepositoryWithPrs()
    {
        var now = DateTime.UtcNow;
        _workoutRepository.Setup(x => x.GetByIdAsync("session-1", default)).ReturnsAsync(new WorkoutSessionDocument
        {
            Id = "session-1",
            TargetUserId = 20,
            ExecutedByUserId = 20,
            StartedAtUtc = now.AddMinutes(-45),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Bench Press",
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 6, Load = 110, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Intensity = new IntensityDocumentValueObject { Type = IntensityType.Rpe, Value = 9 }, RestSeconds = 120 }]
                }
            ]
        });
        _workoutRepository.Setup(x => x.GetCompletedByUserInRangeAsync(20, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(
            [
                new WorkoutSessionDocument
                {
                    Id = "history-1",
                    TargetUserId = 20,
                    ExecutedByUserId = 20,
                    IsCompleted = true,
                    StartedAtUtc = now.AddDays(-2),
                    Exercises =
                    [
                        new ExecutedExerciseDocumentValueObject
                        {
                            ExerciseId = 1,
                            ExerciseName = "Bench Press",
                            Sets = [new ExecutedSetDocumentValueObject { Repetitions = 5, Load = 100, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Intensity = new IntensityDocumentValueObject { Type = IntensityType.Rpe, Value = 8 }, RestSeconds = 120 }]
                        }
                    ]
                }
            ]);

        var handler = new CompleteWorkoutSessionHandler(_workoutRepository.Object, new CompleteWorkoutSessionCommandValidator());
        var result = await handler.HandleAsync(new CompleteWorkoutSessionCommand("session-1", now, 8), 20, default);

        Assert.True(result.IsSuccess);
        _workoutRepository.Verify(x => x.UpdateCompletionAsync(
            "session-1",
            now,
            8,
            It.Is<List<WorkoutPrDocumentValueObject>>(prs => prs.Any(pr => pr.Type == "max_load") && prs.Any(pr => pr.Type == "max_volume")),
            default), Times.Once);
    }

    // --- time-based-exercises: TBE-06 best_pace PR (T21, mirrors T20) ---

    [Fact]
    public async Task CompleteWorkoutSessionHandler_WhenTimeBasedSetHasBetterPaceThanHistory_AddsExactlyOneBestPacePr()
    {
        var now = DateTime.UtcNow;
        _workoutRepository.Setup(x => x.GetByIdAsync("session-pace-1", default)).ReturnsAsync(new WorkoutSessionDocument
        {
            Id = "session-pace-1",
            TargetUserId = 20,
            ExecutedByUserId = 20,
            StartedAtUtc = now.AddMinutes(-30),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 5,
                    ExerciseName = "Running",
                    ExerciseType = ExerciseType.TimeBased,
                    Sets = [new ExecutedSetDocumentValueObject { DurationSeconds = 300, DistanceMeters = 1000m }]
                }
            ]
        });
        _workoutRepository.Setup(x => x.GetCompletedByUserInRangeAsync(20, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync(
            [
                new WorkoutSessionDocument
                {
                    Id = "history-pace-1",
                    TargetUserId = 20,
                    ExecutedByUserId = 20,
                    IsCompleted = true,
                    StartedAtUtc = now.AddDays(-2),
                    Exercises =
                    [
                        new ExecutedExerciseDocumentValueObject
                        {
                            ExerciseId = 5,
                            ExerciseName = "Running",
                            ExerciseType = ExerciseType.TimeBased,
                            Sets = [new ExecutedSetDocumentValueObject { DurationSeconds = 400, DistanceMeters = 1000m }]
                        }
                    ]
                }
            ]);

        var handler = new CompleteWorkoutSessionHandler(_workoutRepository.Object, new CompleteWorkoutSessionCommandValidator());
        var result = await handler.HandleAsync(new CompleteWorkoutSessionCommand("session-pace-1", now, 8), 20, default);

        Assert.True(result.IsSuccess);
        _workoutRepository.Verify(x => x.UpdateCompletionAsync(
            "session-pace-1",
            now,
            8,
            It.Is<List<WorkoutPrDocumentValueObject>>(prs =>
                prs.Count(pr => pr.Type == "best_pace") == 1 &&
                prs.Single(pr => pr.Type == "best_pace").Value == 0.3m &&
                prs.Single(pr => pr.Type == "best_pace").ExerciseId == 5),
            default), Times.Once);
    }

    [Fact]
    public async Task CompleteWorkoutSessionHandler_WhenTimeBasedSetHasNoDistance_AddsNoPrForThatSet()
    {
        var now = DateTime.UtcNow;
        _workoutRepository.Setup(x => x.GetByIdAsync("session-pace-2", default)).ReturnsAsync(new WorkoutSessionDocument
        {
            Id = "session-pace-2",
            TargetUserId = 20,
            ExecutedByUserId = 20,
            StartedAtUtc = now.AddMinutes(-30),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 6,
                    ExerciseName = "Stretching",
                    ExerciseType = ExerciseType.TimeBased,
                    Sets = [new ExecutedSetDocumentValueObject { DurationSeconds = 600, DistanceMeters = null }]
                }
            ]
        });
        _workoutRepository.Setup(x => x.GetCompletedByUserInRangeAsync(20, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        var handler = new CompleteWorkoutSessionHandler(_workoutRepository.Object, new CompleteWorkoutSessionCommandValidator());
        var result = await handler.HandleAsync(new CompleteWorkoutSessionCommand("session-pace-2", now, 8), 20, default);

        Assert.True(result.IsSuccess);
        _workoutRepository.Verify(x => x.UpdateCompletionAsync(
            "session-pace-2",
            now,
            8,
            It.Is<List<WorkoutPrDocumentValueObject>>(prs => prs.Count == 0),
            default), Times.Once);
    }

    [Fact]
    public async Task CompleteWorkoutSessionHandler_WhenMixedSessionWithWeightBasedAndTimeBasedExercises_ProducesBothPrTypesWithoutCrossover()
    {
        var now = DateTime.UtcNow;
        _workoutRepository.Setup(x => x.GetByIdAsync("session-mixed-1", default)).ReturnsAsync(new WorkoutSessionDocument
        {
            Id = "session-mixed-1",
            TargetUserId = 20,
            ExecutedByUserId = 20,
            StartedAtUtc = now.AddMinutes(-30),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Bench Press",
                    ExerciseType = ExerciseType.WeightBased,
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 6, Load = 110m, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, RestSeconds = 120 }]
                },
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 5,
                    ExerciseName = "Running",
                    ExerciseType = ExerciseType.TimeBased,
                    Sets = [new ExecutedSetDocumentValueObject { DurationSeconds = 300, DistanceMeters = 1000m }]
                }
            ]
        });
        _workoutRepository.Setup(x => x.GetCompletedByUserInRangeAsync(20, It.IsAny<DateTime>(), It.IsAny<DateTime>(), default))
            .ReturnsAsync([]);

        var handler = new CompleteWorkoutSessionHandler(_workoutRepository.Object, new CompleteWorkoutSessionCommandValidator());
        var result = await handler.HandleAsync(new CompleteWorkoutSessionCommand("session-mixed-1", now, 8), 20, default);

        Assert.True(result.IsSuccess);
        _workoutRepository.Verify(x => x.UpdateCompletionAsync(
            "session-mixed-1",
            now,
            8,
            It.Is<List<WorkoutPrDocumentValueObject>>(prs =>
                prs.Any(pr => pr.ExerciseId == 1 && pr.Type == "max_volume") &&
                prs.Any(pr => pr.ExerciseId == 1 && pr.Type == "max_load") &&
                prs.Any(pr => pr.ExerciseId == 5 && pr.Type == "best_pace") &&
                prs.All(pr => !(pr.ExerciseId == 5 && pr.Type != "best_pace")) &&
                prs.All(pr => !(pr.ExerciseId == 1 && pr.Type == "best_pace"))),
            default), Times.Once);
    }

    [Fact]
    public async Task GetWorkoutSessionsByUserHandler_ForDifferentUser_ReturnsForbidden()
    {
        _accessPolicy.Setup(x => x.CanCreateWorkoutForAsync(99, 20, default)).ReturnsAsync(false);
        var handler = new GetWorkoutSessionsByUserHandler(_workoutRepository.Object, _accessPolicy.Object, new WorkoutSessionResponseMapper());
        var result = await handler.HandleAsync(new GetWorkoutSessionsByUserQuery(20, null, 10), 99, default);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task GetWorkoutSessionsByUserHandler_InvalidCursor_ReturnsValidationFailure()
    {
        var handler = new GetWorkoutSessionsByUserHandler(_workoutRepository.Object, _accessPolicy.Object, new WorkoutSessionResponseMapper());
        var result = await handler.HandleAsync(new GetWorkoutSessionsByUserQuery(20, "invalid", 10), 20, default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }
}


