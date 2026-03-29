using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Shared.Enums;
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
        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-1",
            DateTime.UtcNow,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 30, LoadUnit.Kg, SetType.Working, 8, 90)])]);

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

        var sut = new UpdateWorkoutExecutionStateHandler(sessionRepository.Object, exerciseRepository.Object, new UpdateWorkoutExecutionStateCommandValidator());

        var command = new UpdateWorkoutExecutionStateCommand(
            "session-2",
            new DateTime(2026, 3, 29, 11, 30, 0, DateTimeKind.Utc),
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 32.5m, LoadUnit.Kg, SetType.Working, 8, 90, true)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedExercises);
        Assert.Single(capturedExercises!);
        Assert.True(capturedExercises![0].Sets[0].IsExtra);
        Assert.Equal(LoadUnit.Kg, capturedExercises[0].Sets[0].LoadUnit);
    }

}



