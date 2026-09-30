using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.MarkWorkoutSet;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.Workouts;

public class MarkWorkoutSetHandlerTests
{
    private static readonly WorkoutSetValueObject Set = new(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90);

    private static MarkWorkoutSetHandler CreateSut(Mock<IWorkoutSessionRepository> sessions, Mock<IExerciseRepository>? exercises = null)
    {
        exercises ??= new Mock<IExerciseRepository>();
        exercises
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });
        return new MarkWorkoutSetHandler(sessions.Object, exercises.Object, new WorkoutSessionResponseMapper(), new MarkWorkoutSetCommandValidator());
    }

    [Fact]
    public async Task HandleAsync_WhenOperationAlreadyApplied_ReturnsSuccessWithoutAppending()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument
            {
                Id = "s1",
                TargetUserId = 10,
                ExecutedByUserId = 10,
                AppliedSetOperationIds = ["op-1"]
            });

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        sessions.Verify(x => x.AppendSetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ExecutedExerciseDocumentValueObject>(), It.IsAny<ExecutedSetDocumentValueObject>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenNewOperation_AppendsSetWithOperationId()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10 });
        sessions
            .Setup(x => x.AppendSetAsync("s1", "op-2", It.Is<ExecutedExerciseDocumentValueObject>(e => e.ExerciseId == 1 && e.ExerciseName == "Bench Press"), It.IsAny<ExecutedSetDocumentValueObject>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-2", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        sessions.Verify(x => x.AppendSetAsync("s1", "op-2", It.IsAny<ExecutedExerciseDocumentValueObject>(), It.Is<ExecutedSetDocumentValueObject>(s => s.Repetitions == 10 && s.Load == 30), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenConcurrentDuplicateWinsRace_ReturnsSuccess()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .SetupSequence(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10 })
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10, AppliedSetOperationIds = ["op-3"] });
        sessions
            .Setup(x => x.AppendSetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ExecutedExerciseDocumentValueObject>(), It.IsAny<ExecutedSetDocumentValueObject>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-3", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WhenActorCannotAccessSession_ReturnsForbidden()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10 });

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 1, Set), 999, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenOperationIdMissing_ReturnsValidationError()
    {
        var result = await CreateSut(new Mock<IWorkoutSessionRepository>()).HandleAsync(new MarkWorkoutSetCommand("s1", "", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionClosedAndOperationNew_ReturnsConflict()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10, IsCompleted = true });

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionClosedAndOperationAlreadyApplied_ReturnsSuccess()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10, IsCompleted = true, AppliedSetOperationIds = ["op-1"] });

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionClosedDuringWrite_ReturnsConflict()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .SetupSequence(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10 })
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10, IsCompleted = true });
        sessions
            .Setup(x => x.AppendSetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ExecutedExerciseDocumentValueObject>(), It.IsAny<ExecutedSetDocumentValueObject>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenSessionMissing_ReturnsNotFound()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkoutSessionDocument?)null);

        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenOperationIdTooLong_ReturnsValidationError()
    {
        var result = await CreateSut(new Mock<IWorkoutSessionRepository>()).HandleAsync(new MarkWorkoutSetCommand("s1", new string('x', 101), 1, Set), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseRequiresRpeAndSetHasNone_ReturnsValidationError()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument
            {
                Id = "s1",
                TargetUserId = 10,
                ExecutedByUserId = 10,
                Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press", RequireRpe = true }]
            });

        var withoutRpe = Set with { Intensity = null };
        var result = await CreateSut(sessions).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 1, withoutRpe), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenTimeBasedExerciseHasNoDuration_ReturnsValidationError()
    {
        var sessions = new Mock<IWorkoutSessionRepository>();
        sessions
            .Setup(x => x.GetByIdAsync("s1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument { Id = "s1", TargetUserId = 10, ExecutedByUserId = 10 });
        var exercises = new Mock<IExerciseRepository>();
        exercises
            .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 2, Name = "Plank", NamePt = "Prancha", ExerciseType = ExerciseType.TimeBased });

        var timeSet = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var result = await CreateSut(sessions, exercises).HandleAsync(new MarkWorkoutSetCommand("s1", "op-1", 2, timeSet), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }
}
