using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.FinishWorkoutExecution;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace UnitTests.Domains.Training.Workouts;

public class FinishWorkoutExecutionHandlerTests
{
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

        var sut = new FinishWorkoutExecutionHandler(sessionRepository.Object, new FinishWorkoutExecutionCommandValidator());

        var result = await sut.HandleAsync(new FinishWorkoutExecutionCommand("session-1", DateTime.UtcNow, 8, null), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(409, result.Error!.StatusCode);
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
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 8, Load = 80m, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Rpe = 8, RestSeconds = 120 }]
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

        var sut = new FinishWorkoutExecutionHandler(sessionRepository.Object, new FinishWorkoutExecutionCommandValidator());

        var command = new FinishWorkoutExecutionCommand(
            "session-2",
            new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc),
            9,
            [new WorkoutExerciseDto(1, [new WorkoutSetValueObject(10, 82.5m, LoadUnit.Kg, SetType.Working, 9, 120, true)])]);

        var result = await sut.HandleAsync(command, 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        sessionRepository.Verify(x => x.UpdateStateAsync("session-2", command.EndedAtUtc, It.IsAny<List<ExecutedExerciseDocumentValueObject>>(), It.IsAny<CancellationToken>()), Times.Once);
        sessionRepository.Verify(x => x.UpdateCompletionAsync("session-2", command.EndedAtUtc, 9, It.IsAny<List<WorkoutPrDocumentValueObject>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

}




