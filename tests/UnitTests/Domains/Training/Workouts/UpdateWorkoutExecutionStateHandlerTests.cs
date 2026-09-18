using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;
using ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

namespace UnitTests.Domains.Training.Workouts;

public class UpdateWorkoutExecutionStateHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenActorCannotAccessSession_ReturnsForbidden()
    {
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument
            {
                Id = "session-1",
                TargetUserId = 10,
                ExecutedByUserId = 10,
                IsCompleted = false
            });

        var exerciseRepository = new Mock<IExerciseRepository>();
        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-1",
            DateTime.UtcNow,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90)])]);

        var result = await sut.HandleAsync(command, 999, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValid_PersistsStateWithExtraSets()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-2",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press" }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        List<ExecutedExerciseDocumentValueObject>? capturedExercises = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("session-2", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => capturedExercises = exercises)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-2",
            new DateTime(2026, 3, 29, 11, 30, 0, DateTimeKind.Utc),
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 32.5m, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90, true)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedExercises);
        Assert.Single(capturedExercises!);
        Assert.True(capturedExercises![0].Sets[0].IsExtra);
        Assert.Equal(LoadUnit.Kg, capturedExercises[0].Sets[0].LoadUnit);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseRequiresRpeAndIntensityMissing_ReturnsValidationErrorWithoutPersisting()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-3",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press", RequireRpe = true }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-3",
            DateTime.UtcNow,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, null, 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        sessionRepository.Verify(
            x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseRequiresRpeAndIntensityProvided_PersistsAndCarriesRequireRpeForward()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-4",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press", RequireRpe = true }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        List<ExecutedExerciseDocumentValueObject>? capturedExercises = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("session-4", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => capturedExercises = exercises)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-4",
            new DateTime(2026, 3, 29, 11, 30, 0, DateTimeKind.Utc),
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedExercises);
        Assert.True(capturedExercises![0].RequireRpe);
        Assert.NotNull(capturedExercises[0].Sets[0].Intensity);
        Assert.Equal(8, capturedExercises[0].Sets[0].Intensity!.Value);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseDoesNotRequireRpeAndIntensityMissing_PersistsSuccessfully()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-5",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press" }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        List<ExecutedExerciseDocumentValueObject>? capturedExercises = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("session-5", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => capturedExercises = exercises)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-5",
            new DateTime(2026, 3, 29, 11, 30, 0, DateTimeKind.Utc),
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, null, 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedExercises);
        Assert.False(capturedExercises![0].RequireRpe);
        Assert.Null(capturedExercises[0].Sets[0].Intensity);
    }

    [Fact]
    public async Task HandleAsync_WhenSetHasInvalidFormat_ReturnsValidationErrorWithoutPersisting()
    {
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        var exerciseRepository = new Mock<IExerciseRepository>();

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-6",
            DateTime.UtcNow,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(null, -5, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        sessionRepository.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        sessionRepository.Verify(
            x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // --- time-based-exercises: TBE-03 backend completion gate (T19) ---

    [Fact]
    public async Task HandleAsync_WhenTimeBasedExerciseSetMissingDuration_ReturnsValidationErrorNamingExercise()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-7",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Running" }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-7", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Running", NamePt = "Corrida", ExerciseType = ExerciseType.TimeBased });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var timeBasedSetMissingDuration = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null);
        var command = new UpdateWorkoutExecutionStateCommand(
            "session-7",
            DateTime.UtcNow,
            [new WorkoutExerciseDto(1, [timeBasedSetMissingDuration])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        Assert.Contains("'1'", result.Error.Message, StringComparison.Ordinal);
        sessionRepository.Verify(
            x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public async Task HandleAsync_WhenTimeBasedExerciseSetHasZeroOrNegativeDuration_ReturnsValidationError(int durationSeconds)
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-8",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Running" }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-8", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Running", NamePt = "Corrida", ExerciseType = ExerciseType.TimeBased });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var timeBasedSet = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null, DurationSeconds: durationSeconds);
        var command = new UpdateWorkoutExecutionStateCommand(
            "session-8",
            DateTime.UtcNow,
            [new WorkoutExerciseDto(1, [timeBasedSet])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenTimeBasedExerciseSetHasValidDurationWithoutDistance_PersistsSuccessfully()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-9",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Stretching" }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-9", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        List<ExecutedExerciseDocumentValueObject>? capturedExercises = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("session-9", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => capturedExercises = exercises)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Stretching", NamePt = "Alongamento", ExerciseType = ExerciseType.TimeBased });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var timeBasedSet = new WorkoutSetValueObject(null, null, LoadUnit.Kg, SetType.Working, Technique.Straight, null, null, DurationSeconds: 90);
        var command = new UpdateWorkoutExecutionStateCommand(
            "session-9",
            new DateTime(2026, 3, 29, 11, 30, 0, DateTimeKind.Utc),
            [new WorkoutExerciseDto(1, [timeBasedSet])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedExercises);
        Assert.Equal(90, capturedExercises![0].Sets[0].DurationSeconds);
        Assert.Null(capturedExercises[0].Sets[0].DistanceMeters);
    }

    [Fact]
    public async Task HandleAsync_WhenWeightBasedExerciseSetHasNoDuration_PersistsSuccessfullyUnaffectedByTimeBasedGate()
    {
        // Regression: the new TimeBased gate must never engage for a WeightBased exercise --
        // WEV-01/02 behavior stays exactly as it was before this feature.
        var session = new WorkoutSessionDocument
        {
            Id = "session-10",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press" }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-10", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        List<ExecutedExerciseDocumentValueObject>? capturedExercises = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("session-10", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => capturedExercises = exercises)
            .Returns(Task.CompletedTask);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino" });

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new WorkoutSessionResponseMapper(), new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-10",
            new DateTime(2026, 3, 29, 11, 30, 0, DateTimeKind.Utc),
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedExercises);
        Assert.Null(capturedExercises![0].Sets[0].DurationSeconds);
    }

}



