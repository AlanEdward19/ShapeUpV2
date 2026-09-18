using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Entities;
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
        var exerciseRepository = new Mock<IExerciseRepository>();
        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, exerciseRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

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

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino", ExerciseType = ExerciseType.WeightBased });

        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, exerciseRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

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
        Assert.Equal(ExerciseType.WeightBased, captured.Exercises[0].ExerciseType);
    }

    [Fact]
    public async Task HandleAsync_WhenPlanHasExercisesWithMixedRequireRpe_CarriesFlagIntoSnapshot()
    {
        var plan = new WorkoutPlanDocument
        {
            Id = "plan-rpe",
            TargetUserId = 30,
            CreatedByUserId = 10,
            Name = "Plan RPE",
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
                            RequireRpe = true,
                            Sets = [new PlannedSetDocumentValueObject { Repetitions = 8, Load = 80, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Intensity = new IntensityDocumentValueObject { Type = IntensityType.Rpe, Value = 8 }, RestSeconds = 120 }]
                        },
                        new BlockExerciseDocumentValueObject
                        {
                            ExerciseId = 2,
                            ExerciseName = "Squat",
                            Sets = [new PlannedSetDocumentValueObject { Repetitions = 5, Load = 100, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, Intensity = null, RestSeconds = 180 }]
                        }
                    ]
                }
            ]
        };

        var planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository
            .Setup(x => x.GetByIdAsync("plan-rpe", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        WorkoutSessionDocument? captured = null;
        sessionRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutSessionDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutSessionDocument, CancellationToken>((doc, _) =>
            {
                doc.Id = "session-rpe";
                captured = doc;
            })
            .Returns(Task.CompletedTask);

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino", ExerciseType = ExerciseType.WeightBased });
        exerciseRepository
            .Setup(x => x.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 2, Name = "Squat", NamePt = "Agachamento", ExerciseType = ExerciseType.WeightBased });

        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, exerciseRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

        var result = await sut.HandleAsync(new StartWorkoutExecutionCommand("plan-rpe", DateTime.UtcNow, null), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        var benchPress = captured!.Exercises.Single(e => e.ExerciseId == 1);
        var squat = captured.Exercises.Single(e => e.ExerciseId == 2);
        Assert.True(benchPress.RequireRpe);
        Assert.False(squat.RequireRpe);
        Assert.Equal(ExerciseType.WeightBased, benchPress.ExerciseType);
        Assert.Equal(ExerciseType.WeightBased, squat.ExerciseType);
    }

    [Fact]
    public async Task HandleAsync_WhenPlanHasTimeBasedExercise_FlattensExerciseTypeAndDurationOntoSnapshot()
    {
        var plan = new WorkoutPlanDocument
        {
            Id = "plan-time",
            TargetUserId = 30,
            CreatedByUserId = 10,
            Name = "Plan Time",
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
                            ExerciseId = 3,
                            ExerciseName = "Running",
                            Sets = [new PlannedSetDocumentValueObject { DurationSeconds = 600, DistanceMeters = 2000m, RestSeconds = 60 }]
                        },
                        new BlockExerciseDocumentValueObject
                        {
                            ExerciseId = 1,
                            ExerciseName = "Bench Press",
                            Sets = [new PlannedSetDocumentValueObject { Repetitions = 8, Load = 80, LoadUnit = LoadUnit.Kg, SetType = SetType.Working, RestSeconds = 120 }]
                        }
                    ]
                }
            ]
        };

        var planRepository = new Mock<IWorkoutPlanRepository>();
        planRepository
            .Setup(x => x.GetByIdAsync("plan-time", It.IsAny<CancellationToken>()))
            .ReturnsAsync(plan);

        var sessionRepository = new Mock<IWorkoutSessionRepository>();
        WorkoutSessionDocument? captured = null;
        sessionRepository
            .Setup(x => x.AddAsync(It.IsAny<WorkoutSessionDocument>(), It.IsAny<CancellationToken>()))
            .Callback<WorkoutSessionDocument, CancellationToken>((doc, _) =>
            {
                doc.Id = "session-time";
                captured = doc;
            })
            .Returns(Task.CompletedTask);

        var accessPolicy = new Mock<ITrainingAccessPolicy>();
        accessPolicy
            .Setup(x => x.CanCreateWorkoutForAsync(10, 30, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exerciseRepository = new Mock<IExerciseRepository>();
        exerciseRepository
            .Setup(x => x.GetByIdAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 3, Name = "Running", NamePt = "Corrida", ExerciseType = ExerciseType.TimeBased });
        exerciseRepository
            .Setup(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Exercise { Id = 1, Name = "Bench Press", NamePt = "Supino", ExerciseType = ExerciseType.WeightBased });

        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, exerciseRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

        var result = await sut.HandleAsync(new StartWorkoutExecutionCommand("plan-time", DateTime.UtcNow, null), 10, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        var running = captured!.Exercises.Single(e => e.ExerciseId == 3);
        var benchPress = captured.Exercises.Single(e => e.ExerciseId == 1);

        Assert.Equal(ExerciseType.TimeBased, running.ExerciseType);
        Assert.Equal(600, running.Sets[0].DurationSeconds);
        Assert.Equal(2000m, running.Sets[0].DistanceMeters);
        Assert.Null(running.Sets[0].Repetitions);

        Assert.Equal(ExerciseType.WeightBased, benchPress.ExerciseType);
        Assert.Equal(8, benchPress.Sets[0].Repetitions);
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

        var exerciseRepository = new Mock<IExerciseRepository>();
        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, exerciseRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

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
        var exerciseRepository = new Mock<IExerciseRepository>();
        var sut = new StartWorkoutExecutionHandler(planRepository.Object, sessionRepository.Object, exerciseRepository.Object, accessPolicy.Object, new WorkoutSessionResponseMapper(), new StartWorkoutExecutionCommandValidator());

        var result = await sut.HandleAsync(new StartWorkoutExecutionCommand("plan-1", DateTime.UtcNow, null, invalidId), 10, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(400, result.Error!.StatusCode);
    }
}



