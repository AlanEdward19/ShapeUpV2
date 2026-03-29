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
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 6, Load = 110, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Rpe = 9, RestSeconds = 120 }]
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
                            Sets = [new ExecutedSetDocumentValueObject { Repetitions = 5, Load = 100, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Rpe = 8, RestSeconds = 120 }]
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

    [Fact]
    public async Task GetWorkoutSessionsByUserHandler_ForDifferentUser_ReturnsForbidden()
    {
        _accessPolicy.Setup(x => x.CanCreateWorkoutForAsync(99, 20, It.IsAny<string[]>(), default)).ReturnsAsync(false);
        var handler = new GetWorkoutSessionsByUserHandler(_workoutRepository.Object, _accessPolicy.Object, new WorkoutSessionResponseMapper());
        var result = await handler.HandleAsync(new GetWorkoutSessionsByUserQuery(20, null, 10), 99, ["training:workouts:read"], default);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task GetWorkoutSessionsByUserHandler_InvalidCursor_ReturnsValidationFailure()
    {
        var handler = new GetWorkoutSessionsByUserHandler(_workoutRepository.Object, _accessPolicy.Object, new WorkoutSessionResponseMapper());
        var result = await handler.HandleAsync(new GetWorkoutSessionsByUserQuery(20, "invalid", 10), 20, ["training:workouts:read"], default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }
}


