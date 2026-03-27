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

    [Theory]
    [InlineData("Chest", "Peito")]
    [InlineData("Back", "Costas")]
    [InlineData("Legs", "Pernas")]
    [InlineData("Shoulders", "Ombros")]
    public async Task CreateMuscleHandler_ShouldPersist(string muscleName, string musclePt)
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var handler = new CreateMuscleHandler(new MuscleRepository(ctx), new CreateMuscleCommandValidator());

        var result = await handler.HandleAsync(new CreateMuscleCommand($"{muscleName}-{Guid.NewGuid():N}", $"{musclePt}-{Guid.NewGuid():N}"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Id > 0);
    }

    [Theory]
    [InlineData("Bench Press", "Supino")]
    [InlineData("Squat", "Agachamento")]
    [InlineData("Deadlift", "Rosca")]
    public async Task CreateExerciseHandler_ShouldPersistRelationships(string exerciseName, string exercisePt)
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
                $"{exerciseName}-{Guid.NewGuid():N}",
                $"{exercisePt}-{Guid.NewGuid():N}",
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

    [Theory]
    [InlineData(70, 80, 90)]
    [InlineData(50, 50)]
    [InlineData(100)]
    public async Task CreateExerciseHandler_ShouldHandleMultipleMusclesAndEquipments_WithVariousActivationPercents(params decimal[] activationPercents)
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var muscleHandler = new CreateMuscleHandler(new MuscleRepository(ctx), new CreateMuscleCommandValidator());
        var equipmentHandler = new CreateEquipmentHandler(new EquipmentRepository(ctx), new CreateEquipmentCommandValidator());

        var muscles = new List<ExerciseMuscleDto>();
        for (int i = 0; i < activationPercents.Length; i++)
        {
            var muscle = await muscleHandler.HandleAsync(
                new CreateMuscleCommand($"Muscle{i}-{Guid.NewGuid():N}", $"Músculo{i}-{Guid.NewGuid():N}"),
                CancellationToken.None);
            muscles.Add(new ExerciseMuscleDto(muscle.Value!.Id, activationPercents[i]));
        }

        var equipment1 = await equipmentHandler.HandleAsync(
            new CreateEquipmentCommand($"Equipment1-{Guid.NewGuid():N}", $"Equipamento1-{Guid.NewGuid():N}", null),
            CancellationToken.None);
        var equipment2 = await equipmentHandler.HandleAsync(
            new CreateEquipmentCommand($"Equipment2-{Guid.NewGuid():N}", $"Equipamento2-{Guid.NewGuid():N}", null),
            CancellationToken.None);

        var handler = new CreateExerciseHandler(
            new ExerciseRepository(ctx),
            new EquipmentRepository(ctx),
            new MuscleRepository(ctx),
            new CreateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new CreateExerciseCommand(
                $"Complex Exercise-{Guid.NewGuid():N}",
                $"Exercício Complexo-{Guid.NewGuid():N}",
                "Multiple muscles",
                null,
                muscles.ToArray(),
                [equipment1.Value!.Id, equipment2.Value!.Id],
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(activationPercents.Length, result.Value!.Muscles.Length);
        Assert.Equal(2, result.Value.Equipments.Length);
    }

    [Theory]
    [InlineData("working", 8, 100, 8)]
    [InlineData("warmup", 12, 50, 3)]
    [InlineData("topset", 5, 120, 9)]
    [InlineData("dropset", 10, 80, 8)]
    public async Task CreateWorkoutAndCompleteHandlers_ShouldPersistMongoFlowAndDashboardData(string setType, int reps, decimal load, int rpe)
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
                [new WorkoutExerciseDto(exercise.Value!.Id, [new WorkoutSetValueObject(reps, load, "kg", setType, rpe, 120)])]),
            200,
            ["training:workouts:create:self"],
            CancellationToken.None);

        Assert.True(created.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(created.Value!.SessionId));

        var completeHandler = new CompleteWorkoutSessionHandler(mongoRepository, new CompleteWorkoutSessionCommandValidator());
        var completed = await completeHandler.HandleAsync(
            new CompleteWorkoutSessionCommand(created.Value.SessionId, DateTime.UtcNow, rpe),
            200,
            CancellationToken.None);

        Assert.True(completed.IsSuccess);

        var dashboardHandler = new GetTrainingDashboardHandler(mongoRepository);
        var dashboard = await dashboardHandler.HandleAsync(new GetTrainingDashboardQuery(200, 3), CancellationToken.None);

        Assert.True(dashboard.IsSuccess);
        Assert.True(dashboard.Value!.WeeklyVolume >= 0);
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


