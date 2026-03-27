namespace IntegrationTests.Domains.Training.Handlers;

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;
using ShapeUp.Features.Training.Equipments.CreateEquipment;
using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.Dtos;
using ShapeUp.Features.Training.Infrastructure.Mongo;
using ShapeUp.Features.Training.Infrastructure.Repositories;
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

    // Muscles are now an enum (EMuscleGroup), so CreateMuscleHandler no longer exists.
    // This test is kept for documentation purposes but commented out.
    // 
    // [Theory]
    // [InlineData("Chest", "Peito")]
    // [InlineData("Back", "Costas")]
    // [InlineData("Legs", "Pernas")]
    // [InlineData("Shoulders", "Ombros")]
    // public async Task CreateMuscleHandler_ShouldPersist(string muscleName, string musclePt)
    // {
    //     await using var ctx = fixture.CreateTrainingDbContext();
    //     var handler = new CreateMuscleHandler(new MuscleRepository(ctx), new CreateMuscleCommandValidator());
    //     var result = await handler.HandleAsync(new CreateMuscleCommand($"{muscleName}-{Guid.NewGuid():N}", $"{musclePt}-{Guid.NewGuid():N}"), CancellationToken.None);
    //     Assert.True(result.IsSuccess);
    //     Assert.True(result.Value!.Id > 0);
    // }

    [Theory]
    [InlineData("Bench Press", "Supino")]
    [InlineData("Squat", "Agachamento")]
    [InlineData("Deadlift", "Rosca")]
    public async Task CreateExerciseHandler_ShouldPersistRelationships(string exerciseName, string exercisePt)
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var equipmentHandler = new CreateEquipmentHandler(new EquipmentRepository(ctx), new CreateEquipmentCommandValidator());

        var equipment = await equipmentHandler.HandleAsync(new CreateEquipmentCommand($"Barbell-{Guid.NewGuid():N}", $"Barra-{Guid.NewGuid():N}", null), CancellationToken.None);

        var handler = new CreateExerciseHandler(
            new ExerciseRepository(ctx),
            new EquipmentRepository(ctx),
            new CreateExerciseCommandValidator());

        var result = await handler.HandleAsync(
            new CreateExerciseCommand(
                $"{exerciseName}-{Guid.NewGuid():N}",
                $"{exercisePt}-{Guid.NewGuid():N}",
                "desc",
                null,
                [new ExerciseMuscleDto(ShapeUp.Features.Training.Shared.Enums.EMuscleGroup.Chest, 70)],
                [equipment.Value!.Id],
                [new ExerciseStepDto("Drive feet into the floor")]),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Muscles);
        Assert.Single(result.Value.Equipments);
        Assert.Single(result.Value.Steps);
    }
    // This test was removed because it depended on CreateMuscleHandler which no longer exists.
    // Muscles are now an EMuscleGroup enum instead of a database table.
    //
    // [Theory]
    // [InlineData(70, 80, 90)]
    // public async Task CreateExerciseHandler_ShouldHandleMultipleMusclesAndEquipments_WithVariousActivationPercents(...) { ... }

    // This test was removed because it depended on CreateMuscleHandler which no longer exists.
    // Muscles are now an EMuscleGroup enum instead of a database table.
    //
    // [Theory]
    // [InlineData("working", 8, 100, 8)]
    // public async Task CreateWorkoutAndCompleteHandlers_ShouldPersistMongoFlowAndDashboardData(...) { ... }

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


