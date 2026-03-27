namespace IntegrationTests.Domains.Training.Handlers;

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;
using ShapeUp.Features.Training.Equipments.CreateEquipment;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.Dtos;
using ShapeUp.Features.Training.Infrastructure.Mongo;
using ShapeUp.Features.Training.Infrastructure.Repositories;
using ShapeUp.Features.Training.Muscles.CreateMuscle;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;
using ShapeUp.Features.Training.Workouts.CreateWorkoutSession;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class TrainingHandlerIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateMuscleHandler_ShouldPersist()
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var handler = new CreateMuscleHandler(new MuscleRepository(ctx), new CreateMuscleCommandValidator());

        var result = await handler.HandleAsync(new CreateMuscleCommand($"Chest-{Guid.NewGuid():N}", $"Peito-{Guid.NewGuid():N}"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Id > 0);
    }

    [Fact]
    public async Task CreateExerciseHandler_ShouldPersistRelationships()
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var muscleHandler = new CreateMuscleHandler(new MuscleRepository(ctx), new CreateMuscleCommandValidator());
        var equipmentHandler = new CreateEquipmentHandler(new EquipmentRepository(ctx), new CreateEquipmentCommandValidator());

        var muscle = await muscleHandler.HandleAsync(new CreateMuscleCommand($"Chest-{Guid.NewGuid():N}", $"Peito-{Guid.NewGuid():N}"), CancellationToken.None);
        var equipment = await equipmentHandler.HandleAsync(new CreateEquipmentCommand($"Barbell-{Guid.NewGuid():N}", $"Barra-{Guid.NewGuid():N}", null), CancellationToken.None);

        var handler = new CreateExerciseHandler(
            new ExerciseRepository(ctx),
            new EquipmentRepository(ctx),
            new MuscleRepository(ctx),
            new CreateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new CreateExerciseCommand(
                $"Bench Press-{Guid.NewGuid():N}",
                $"Supino-{Guid.NewGuid():N}",
                "desc",
                null,
                [new ExerciseMuscleDto(muscle.Value!.Id, 70)],
                [equipment.Value!.Id],
                [new ExerciseStepDto("Drive feet into the floor")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Muscles);
        Assert.Single(result.Value.Equipments);
        Assert.Single(result.Value.Steps);
    }

    [Fact]
    public async Task CreateWorkoutAndCompleteHandlers_ShouldPersistMongoFlowAndDashboardData()
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var muscleHandler = new CreateMuscleHandler(new MuscleRepository(ctx), new CreateMuscleCommandValidator());
        var equipmentHandler = new CreateEquipmentHandler(new EquipmentRepository(ctx), new CreateEquipmentCommandValidator());
        var muscle = await muscleHandler.HandleAsync(new CreateMuscleCommand($"Chest-{Guid.NewGuid():N}", $"Peito-{Guid.NewGuid():N}"), CancellationToken.None);
        var equipment = await equipmentHandler.HandleAsync(new CreateEquipmentCommand($"Barbell-{Guid.NewGuid():N}", $"Barra-{Guid.NewGuid():N}", null), CancellationToken.None);

        var exerciseHandler = new CreateExerciseHandler(
            new ExerciseRepository(ctx),
            new EquipmentRepository(ctx),
            new MuscleRepository(ctx),
            new CreateExerciseCommandValidator());
        var exercise = await exerciseHandler.HandleAsync(
            new CreateExerciseCommand(
                $"Bench Press-{Guid.NewGuid():N}",
                $"Supino-{Guid.NewGuid():N}",
                null,
                null,
                [new ExerciseMuscleDto(muscle.Value!.Id, 70)],
                [equipment.Value!.Id],
                null),
            CancellationToken.None);

        var mongoRepository = CreateMongoRepository(fixture.MongoConnectionString, $"training-tests-{Guid.NewGuid():N}");

        var createHandler = new CreateWorkoutSessionHandler(
            mongoRepository,
            new ExerciseRepository(ctx),
            new AllowAllAccessPolicy(),
            new CreateWorkoutSessionCommandValidator());

        var created = await createHandler.HandleAsync(
            new CreateWorkoutSessionCommand(
                200,
                200,
                DateTime.UtcNow.AddMinutes(-35),
                [new WorkoutExerciseDto(exercise.Value!.Id, [new WorkoutSetValueObject(8, 100, "kg", "working", 8, 120)])]),
            200,
            ["training:workouts:create:self"],
            CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(created.Value!.SessionId));

        var completeHandler = new CompleteWorkoutSessionHandler(mongoRepository, new CompleteWorkoutSessionCommandValidator());
        var completed = await completeHandler.HandleAsync(
            new CompleteWorkoutSessionCommand(created.Value.SessionId, DateTime.UtcNow, 8),
            200,
            CancellationToken.None);

        Assert.True(completed.IsSuccess);

        var dashboardHandler = new GetTrainingDashboardHandler(mongoRepository);
        var dashboard = await dashboardHandler.HandleAsync(new GetTrainingDashboardQuery(200, 3), CancellationToken.None);

        Assert.True(dashboard.IsSuccess);
        Assert.True(dashboard.Value!.WeeklyVolume > 0);
        Assert.Equal(1, dashboard.Value.SessionsCompletedThisWeek);
    }

    private static MongoWorkoutSessionRepository CreateMongoRepository(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        var options = Options.Create(new TrainingMongoOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName,
            WorkoutSessionsCollectionName = "workout_sessions"
        });

        return new MongoWorkoutSessionRepository(client, options);
    }

    private sealed class AllowAllAccessPolicy : ITrainingAccessPolicy
    {
        public Task<bool> CanCreateWorkoutForAsync(int actorUserId, int targetUserId, string[] actorScopes, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}


