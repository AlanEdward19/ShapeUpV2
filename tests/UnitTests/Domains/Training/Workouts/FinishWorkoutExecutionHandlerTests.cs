using MassTransit;
using ShapeUp.Configurations;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Shared.Events;
using ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.Workouts;

public class FinishWorkoutExecutionHandlerTests
{
    private static FinishWorkoutExecutionHandler CreateHandler(
        IWorkoutSessionRepository sessionRepository,
        IPublishEndpoint publishEndpoint)
    {
        var outboxTransaction = new Mock<IWorkoutOutboxTransaction>();
        outboxTransaction
            .Setup(x => x.ExecuteAsync(It.IsAny<Func<MongoDB.Driver.IClientSessionHandle, CancellationToken, Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<MongoDB.Driver.IClientSessionHandle, CancellationToken, Task>, CancellationToken>((action, ct) => action(null!, ct));

        return new FinishWorkoutExecutionHandler(
            sessionRepository,
            new FinishWorkoutExecutionCommandValidator(),
            publishEndpoint,
            outboxTransaction.Object,
            new NoOpOutboxFaultInjector());
    }

    [Fact]
    public async Task HandleAsync_WhenSessionAlreadyCompleted_ReturnsConflict()
    {
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WorkoutSessionDocument
            {
                Id = "session-1",
                TargetUserId = 10,
                ExecutedByUserId = 10,
                IsCompleted = true
            });

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var sut = CreateHandler(sessionRepository.Object, publishEndpoint.Object);

        var result = await sut.HandleAsync(new FinishWorkoutExecutionCommand("session-1", DateTime.UtcNow, 8, null), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.StatusCode);
        publishEndpoint.Verify(
            x => x.Publish(It.IsAny<WorkoutFinished>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenExercisesProvided_UpdatesStateBeforeCompleting()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-2",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            StartedAtUtc = new DateTime(2026, 3, 29, 9, 0, 0, DateTimeKind.Utc),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Bench Press",
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 8, Load = 80m, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Technique = Technique.Straight, Intensity = new IntensityDocumentValueObject { Type = IntensityType.Rpe, Value = 8 }, RestSeconds = 120 }]
                }
            ]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var sut = CreateHandler(sessionRepository.Object, publishEndpoint.Object);

        var command = new FinishWorkoutExecutionCommand(
            "session-2",
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            9,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 82.5m, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 9), 120, true)])]);
        var endedAtUtc = command.EndedAtUtc!.Value;

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        sessionRepository.Verify(x => x.UpdateStateAsync("session-2", endedAtUtc, It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()), Times.Once);
        sessionRepository.Verify(x => x.UpdateCompletionAsync("session-2", endedAtUtc, 9, It.IsAny<List<WorkoutPrDocumentValueObject>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenCompletionSucceeds_PublishesWorkoutFinishedWithExpectedPayload()
    {
        var endedAtUtc = new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc);
        var session = new WorkoutSessionDocument
        {
            Id = "session-3",
            TargetUserId = 42,
            ExecutedByUserId = 17,
            IsCompleted = false,
            StartedAtUtc = new DateTime(2026, 3, 29, 9, 0, 0, DateTimeKind.Utc),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Squat",
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 5, Load = 100m, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Technique = Technique.Straight, Intensity = new IntensityDocumentValueObject { Type = IntensityType.Rpe, Value = 8 }, RestSeconds = 180 }]
                }
            ]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(42, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var sut = CreateHandler(sessionRepository.Object, publishEndpoint.Object);

        var result = await sut.HandleAsync(new FinishWorkoutExecutionCommand("session-3", endedAtUtc, 8, null), 17, CancellationToken.None);

        Assert.True(result.IsSuccess);
        publishEndpoint.Verify(
            x => x.Publish(
                It.Is<WorkoutFinished>(e =>
                    e == new WorkoutFinished("session-3", 42, 17, endedAtUtc)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseRequiresRpeAndIntensityMissing_ReturnsValidationErrorWithoutPersisting()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-4",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            StartedAtUtc = new DateTime(2026, 3, 29, 9, 0, 0, DateTimeKind.Utc),
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press", RequireRpe = true }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-4", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var sut = CreateHandler(sessionRepository.Object, publishEndpoint.Object);

        var command = new FinishWorkoutExecutionCommand(
            "session-4",
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            8,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, null, 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        sessionRepository.Verify(
            x => x.UpdateStateAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()),
            Times.Never);
        publishEndpoint.Verify(x => x.Publish(It.IsAny<WorkoutFinished>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WhenExerciseRequiresRpeAndIntensityProvided_PersistsAndCarriesRequireRpeForward()
    {
        var session = new WorkoutSessionDocument
        {
            Id = "session-5",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            StartedAtUtc = new DateTime(2026, 3, 29, 9, 0, 0, DateTimeKind.Utc),
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press", RequireRpe = true }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-5", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<ExecutedExerciseDocumentValueObject>? capturedExercises = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("session-5", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => capturedExercises = exercises)
            .Returns(Task.CompletedTask);

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var sut = CreateHandler(sessionRepository.Object, publishEndpoint.Object);

        var command = new FinishWorkoutExecutionCommand(
            "session-5",
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            8,
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
            Id = "session-6",
            TargetUserId = 10,
            ExecutedByUserId = 10,
            IsCompleted = false,
            StartedAtUtc = new DateTime(2026, 3, 29, 9, 0, 0, DateTimeKind.Utc),
            Exercises = [new ExecutedExerciseDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press" }]
        };

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        sessionRepository
            .Setup(x => x.GetByIdAsync("session-6", It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);
        sessionRepository
            .Setup(x => x.GetCompletedByUserInRangeAsync(10, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        List<ExecutedExerciseDocumentValueObject>? capturedExercises = null;
        sessionRepository
            .Setup(x => x.UpdateStateAsync("session-6", It.IsAny<DateTime>(), It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()))
            .Callback<string, DateTime, List<ExecutedExerciseDocumentValueObject>, CancellationToken>((_, _, exercises, _) => capturedExercises = exercises)
            .Returns(Task.CompletedTask);

        var publishEndpoint = new Mock<IPublishEndpoint>();
        var sut = CreateHandler(sessionRepository.Object, publishEndpoint.Object);

        var command = new FinishWorkoutExecutionCommand(
            "session-6",
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            8,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, Technique.Straight, null, 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedExercises);
        Assert.False(capturedExercises![0].RequireRpe);
        Assert.Null(capturedExercises[0].Sets[0].Intensity);
    }

    [Theory]
    [InlineData(null, 30)]
    [InlineData(10, -5)]
    public async Task HandleAsync_WhenExercisesSetHasInvalidFormat_ReturnsValidationError(int? repetitions, decimal load)
    {
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        var publishEndpoint = new Mock<IPublishEndpoint>();
        var sut = CreateHandler(sessionRepository.Object, publishEndpoint.Object);

        var command = new FinishWorkoutExecutionCommand(
            "session-7",
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            8,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(repetitions, load, LoadUnit.Kg, SetType.Working, Technique.Straight, new IntensityDto(IntensityType.Rpe, 8), 90)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
        sessionRepository.Verify(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

