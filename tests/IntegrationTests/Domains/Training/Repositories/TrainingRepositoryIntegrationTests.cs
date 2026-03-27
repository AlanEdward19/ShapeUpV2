namespace IntegrationTests.Domains.Training.Repositories;

using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Training.Infrastructure.Mongo;
using ShapeUp.Features.Training.Infrastructure.Policies;
using ShapeUp.Features.Training.Infrastructure.Repositories;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Entities;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class TrainingRepositoryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task MuscleRepository_AddAndGetById_ShouldPersist()
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var repo = new MuscleRepository(ctx);

        var muscle = new Muscle { Name = $"Chest-{Guid.NewGuid():N}", NamePt = $"Peito-{Guid.NewGuid():N}" };
        await repo.AddAsync(muscle, CancellationToken.None);

        var found = await repo.GetByIdAsync(muscle.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(muscle.Name, found.Name);
        Assert.Equal(muscle.NamePt, found.NamePt);
    }

    [Fact]
    public async Task EquipmentRepository_GetKeysetPage_ShouldReturnDescendingOrder()
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var repo = new EquipmentRepository(ctx);

        await repo.AddAsync(new Equipment { Name = $"Barbell-{Guid.NewGuid():N}", NamePt = $"Barra-{Guid.NewGuid():N}" }, CancellationToken.None);
        await repo.AddAsync(new Equipment { Name = $"Dumbbell-{Guid.NewGuid():N}", NamePt = $"Haltere-{Guid.NewGuid():N}" }, CancellationToken.None);
        await repo.AddAsync(new Equipment { Name = $"Cable-{Guid.NewGuid():N}", NamePt = $"Cabo-{Guid.NewGuid():N}" }, CancellationToken.None);

        var page = await repo.GetKeysetPageAsync(null, 2, CancellationToken.None);

        Assert.Equal(2, page.Count);
        Assert.True(page[0].Id > page[1].Id);
    }

    [Fact]
    public async Task ExerciseRepository_Suggest_ShouldFilterByLocalizedNameMuscleAndEquipment()
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var exerciseRepo = new ExerciseRepository(ctx);
        var equipmentRepo = new EquipmentRepository(ctx);
        var muscleRepo = new MuscleRepository(ctx);

        var chest = new Muscle { Name = $"Chest-{Guid.NewGuid():N}", NamePt = $"Peito-{Guid.NewGuid():N}" };
        var biceps = new Muscle { Name = $"Biceps-{Guid.NewGuid():N}", NamePt = $"BicepsPt-{Guid.NewGuid():N}" };
        await muscleRepo.AddAsync(chest, CancellationToken.None);
        await muscleRepo.AddAsync(biceps, CancellationToken.None);

        var barbell = new Equipment { Name = $"Barbell-{Guid.NewGuid():N}", NamePt = $"Barra-{Guid.NewGuid():N}" };
        var dumbbell = new Equipment { Name = $"Dumbbell-{Guid.NewGuid():N}", NamePt = $"Haltere-{Guid.NewGuid():N}" };
        await equipmentRepo.AddAsync(barbell, CancellationToken.None);
        await equipmentRepo.AddAsync(dumbbell, CancellationToken.None);

        await exerciseRepo.AddAsync(new Exercise
        {
            Name = $"Bench Press-{Guid.NewGuid():N}",
            NamePt = $"Supino-{Guid.NewGuid():N}",
            MuscleProfiles = [new ExerciseMuscleProfile { MuscleId = chest.Id, ActivationPercent = 70 }],
            ExerciseEquipments = [new ExerciseEquipment { EquipmentId = barbell.Id }]
        }, CancellationToken.None);

        await exerciseRepo.AddAsync(new Exercise
        {
            Name = $"Curl-{Guid.NewGuid():N}",
            NamePt = $"Rosca-{Guid.NewGuid():N}",
            MuscleProfiles = [new ExerciseMuscleProfile { MuscleId = biceps.Id, ActivationPercent = 70 }],
            ExerciseEquipments = [new ExerciseEquipment { EquipmentId = dumbbell.Id }]
        }, CancellationToken.None);

        var suggestions = await exerciseRepo.SuggestAsync("supino", [chest.Id], [barbell.Id], 10, CancellationToken.None);

        Assert.Single(suggestions);
        Assert.Contains("Supino", suggestions[0].NamePt);
    }

    [Fact]
    public async Task TrainingAccessPolicy_ShouldAllowTrainerClientRelation()
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var plan = new TrainerPlan { TrainerId = 1001, Name = $"Plan-{Guid.NewGuid():N}", Price = 10m, DurationDays = 30 };
        ctx.TrainerPlans.Add(plan);
        await ctx.SaveChangesAsync();

        ctx.TrainerClients.Add(new TrainerClient
        {
            TrainerId = 1001,
            ClientId = 2001,
            TrainerPlanId = plan.Id,
            IsActive = true
        });
        await ctx.SaveChangesAsync();

        var policy = new TrainingAccessPolicy(ctx);
        var result = await policy.CanCreateWorkoutForAsync(1001, 2001, ["training:workouts:create:trainer"], CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task MongoWorkoutSessionRepository_AddCompleteAndQuery_ShouldWork()
    {
        var repository = CreateMongoRepository(fixture.MongoConnectionString, $"training-tests-{Guid.NewGuid():N}");

        var session = new WorkoutSessionDocument
        {
            Id = "507f1f77bcf86cd799439011",
            TargetUserId = 15,
            ExecutedByUserId = 15,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-40),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Bench Press",
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 5, Load = 100, LoadUnit = "kg", SetType = "working", Rpe = 8, RestSeconds = 120 }]
                }
            ]
        };

        await repository.AddAsync(session, CancellationToken.None);
        await repository.UpdateCompletionAsync(session.Id, session.StartedAtUtc.AddMinutes(40), 8,
        [
            new WorkoutPrDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press", Type = "max_load", Value = 100 }
        ], CancellationToken.None);

        var found = await repository.GetByIdAsync(session.Id, CancellationToken.None);
        var range = await repository.GetCompletedByUserInRangeAsync(15, session.StartedAtUtc.AddDays(-1), session.StartedAtUtc.AddDays(1), CancellationToken.None);
        var keyset = await repository.GetByTargetUserKeysetAsync(15, null, 10, CancellationToken.None);

        Assert.NotNull(found);
        Assert.True(found.IsCompleted);
        Assert.Equal(8, found.PerceivedExertion);
        Assert.Single(found.PersonalRecords);
        Assert.Single(range);
        Assert.Single(keyset);
        Assert.True(found.DurationSeconds > 0);
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
}


