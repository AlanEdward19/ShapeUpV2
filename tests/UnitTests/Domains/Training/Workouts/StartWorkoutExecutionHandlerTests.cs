using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Enums;
using ShapeUp.Features.Training.Workouts.Shared;
using ShapeUp.Features.Training.Workouts.StartWorkoutExecution;

namespace UnitTests.Domains.Training.Workouts;

public class StartWorkoutExecutionHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenPlanDoesNotExist_ReturnsNotFound()
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository
            .Setup(x => x.GetByIdAsync("plan-404", It.IsAny<CancellationToken>()))
            .ReturnsAsync((WorkoutPlanDocument?)null);

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

        var result = await sut.HandleAsync(new StartWorkoutExecutionCommand("plan-404", DateTime.UtcNow, null), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task HandleAsync_WhenValid_PersistsSessionAndReturnsResponse()
    {
        var plan = new WorkoutPlanDocument
        {
            Id = "plan-1",
            TargetUserId = 30,
            CreatedByUserId = 10,
            Name = "Plan A",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Blocks =
            [
                new BlockDocumentValueObject
                {
                    Type = BlockType.Straight,
                    Exercises =
                    [
                        new BlockExerciseDocumentValueObject
                        {
                            ExerciseId = 1,
                            ExerciseName = "Bench Press",
                            Sets = [new PlannedSetDocumentValueObject { Repetitions = 8, Load = 80, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Intensity = new IntensityDocumentValueObject { Type = IntensityType.Rpe, Value = 8 }, RestSeconds = 120 }]
                        }
                    ]
                }
            ]
        };

        var planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository
            .Setup(x => x.GetByIdAsync("plan-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        WorkoutSessionDocument? captured = null;
        sessionRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutSessionDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutSessionDocument, CancellationToken>((doc, _) =>
            {
                doc.Id = "session-1";
                captured = doc;
            })
            .Returns(Task.CompletedTask);

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

        var startedAt = new DateTime(2026, 3, 29, 10, 0, 0, DateTimeKind.Utc);
        var result = await sut.HandleAsync(new StartWorkoutExecutionCommand("plan-1", startedAt, 31), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("session-1", result.Value!.SessionId);
        Assert.Equal("plan-1", result.Value.WorkoutPlanId);
        Assert.NotNull(captured);
        Assert.Equal(31, captured!.ExecutedByUserId);
        Assert.Equal(startedAt, captured.LastSavedAtUtc);
        Assert.False(captured.Exercises[0].Sets[0].IsExtra);
    }

    [Fact]
    public async Task HandleAsync_WhenCommandHasClientSuppliedId_UsesItAsSessionId()
    {
        var plan = new WorkoutPlanDocument
        {
            Id = "plan-2",
            TargetUserId = 30,
            CreatedByUserId = 10,
            Name = "Plan B",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            Blocks = []
        };

        var planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository
            .Setup(x => x.GetByIdAsync("plan-2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        string? capturedId = null;
        sessionRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutSessionDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutSessionDocument, CancellationToken>((doc, _) => capturedId = doc.Id)
            .Returns(Task.CompletedTask);

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

        const string clientSuppliedId = "507f1f77bcf86cd799439011";
        var result = await sut.HandleAsync(new StartWorkoutExecutionCommand("plan-2", DateTime.UtcNow, null, clientSuppliedId), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(clientSuppliedId, capturedId);
        Assert.Equal(clientSuppliedId, result.Value!.SessionId);
    }

    [Theory]
    [InlineData("not-a-valid-object-id")]
    [InlineData("507f1f77bcf86cd79943901")] // 23 chars, one short
    public async Task HandleAsync_WhenIdIsNotValidObjectIdFormat_ReturnsValidationError(string invalidId)
    {
        var planRepository = new Mock<IWorkoutPlanRepository>();
        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

        var result = await sut.HandleAsync(new StartWorkoutExecutionCommand("plan-1", DateTime.UtcNow, null, invalidId), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }
}



