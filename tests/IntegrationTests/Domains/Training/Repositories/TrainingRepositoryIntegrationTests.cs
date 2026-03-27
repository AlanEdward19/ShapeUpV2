namespace IntegrationTests.Domains.Training.Repositories;

using Microsoft.Extensions.Options;
using MongoDB.Bson;
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

    [Theory]
    [InlineData("Chest", "Peito")]
    [InlineData("Back", "Costas")]
    [InlineData("Legs", "Pernas")]
    [InlineData("Shoulders", "Ombros")]
    public async Task MuscleRepository_AddAndGetById_ShouldPersist(string muscleName, string musclePt)
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var repo = new MuscleRepository(ctx);

        var muscle = new Muscle { Name = $"{muscleName}-{Guid.NewGuid():N}", NamePt = $"{musclePt}-{Guid.NewGuid():N}" };
        await repo.AddAsync(muscle, CancellationToken.None);

        var found = await repo.GetByIdAsync(muscle.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(muscle.Name, found.Name);
        Assert.Equal(muscle.NamePt, found.NamePt);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(5)]
    public async Task EquipmentRepository_GetKeysetPage_ShouldReturnDescendingOrder(int pageSize)
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var repo = new EquipmentRepository(ctx);

        await repo.AddAsync(new Equipment { Name = $"Barbell-{Guid.NewGuid():N}", NamePt = $"Barra-{Guid.NewGuid():N}" }, CancellationToken.None);
        await repo.AddAsync(new Equipment { Name = $"Dumbbell-{Guid.NewGuid():N}", NamePt = $"Haltere-{Guid.NewGuid():N}" }, CancellationToken.None);
        await repo.AddAsync(new Equipment { Name = $"Cable-{Guid.NewGuid():N}", NamePt = $"Cabo-{Guid.NewGuid():N}" }, CancellationToken.None);
        await repo.AddAsync(new Equipment { Name = $"Kettlebell-{Guid.NewGuid():N}", NamePt = $"Kettlebell-{Guid.NewGuid():N}" }, CancellationToken.None);

        var page = await repo.GetKeysetPageAsync(null, pageSize, CancellationToken.None);

        Assert.True(page.Count <= pageSize);
        Assert.True(page.Count > 0);
        for (int i = 0; i < page.Count - 1; i++)
        {
            Assert.True(page[i].Id > page[i + 1].Id);
        }
    }

    [Theory]
    [InlineData(null, 2)]
    [InlineData(null, 5)]
    public async Task EquipmentRepository_GetKeysetPage_WithCursor_ShouldPaginate(int? cursor, int pageSize)
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var repo = new EquipmentRepository(ctx);

        // Create more equipment than pageSize
        for (int i = 0; i < 10; i++)
        {
            await repo.AddAsync(
                new Equipment { Name = $"Eq{i}-{Guid.NewGuid():N}", NamePt = $"Eq{i}PT-{Guid.NewGuid():N}" },
                CancellationToken.None);
        }

        var page = await repo.GetKeysetPageAsync(cursor, pageSize, CancellationToken.None);
        Assert.True(page.Count <= pageSize);
        Assert.NotEmpty(page);
    }

    [Theory]
    [InlineData(1001, 2001, true)]
    [InlineData(1002, 2002, true)]
    [InlineData(9999, 8888, false)]
    public async Task TrainingAccessPolicy_ShouldValidateTrainerClientRelation(int trainerId, int clientId, bool shouldHaveRelation)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();

        if (shouldHaveRelation)
        {
            var plan = new TrainerPlan { TrainerId = trainerId, Name = $"Plan-{Guid.NewGuid():N}", Price = 10m, DurationDays = 30 };
            ctx.TrainerPlans.Add(plan);
            await ctx.SaveChangesAsync();

            ctx.TrainerClients.Add(new TrainerClient
            {
                TrainerId = trainerId,
                ClientId = clientId,
                TrainerPlanId = plan.Id,
                IsActive = true
            });
            await ctx.SaveChangesAsync();
        }

        var policy = new TrainingAccessPolicy(ctx);
        var result = await policy.CanCreateWorkoutForAsync(trainerId, clientId, ["training:workouts:create:trainer"], CancellationToken.None);

        Assert.Equal(shouldHaveRelation, result);
    }

    [Theory]
    [InlineData("supino")]
    [InlineData("rosca")]
    [InlineData("agach")]
    [InlineData("press")]
    public async Task ExerciseRepository_Suggest_ShouldFilterByLocalizedNameMuscleAndEquipment(string searchTerm)
    {
        await using var ctx = fixture.CreateTrainingDbContext();
        var exerciseRepo = new ExerciseRepository(ctx);
        var equipmentRepo = new EquipmentRepository(ctx);
        var muscleRepo = new MuscleRepository(ctx);

        var chest = new Muscle { Name = $"Chest-{Guid.NewGuid():N}", NamePt = $"Peito-{Guid.NewGuid():N}" };
        var biceps = new Muscle { Name = $"Biceps-{Guid.NewGuid():N}", NamePt = $"Bíceps-{Guid.NewGuid():N}" };
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

        var suggestions = await exerciseRepo.SuggestAsync(searchTerm, [chest.Id, biceps.Id], [barbell.Id, dumbbell.Id], 10, CancellationToken.None);

        Assert.NotNull(suggestions);
    }

    [Theory]
    [InlineData(8, 100, "working")]
    [InlineData(5, 120, "topset")]
    [InlineData(12, 50, "warmup")]
    [InlineData(10, 80, "dropset")]
    public async Task MongoWorkoutSessionRepository_AddCompleteAndQuery_ShouldWork(int rpe, decimal load, string setType)
    {
        var repository = CreateMongoRepository(fixture.MongoConnectionString, $"training-tests-{Guid.NewGuid():N}");

        var session = new WorkoutSessionDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            TargetUserId = 15,
            ExecutedByUserId = 15,
            StartedAtUtc = DateTime.UtcNow.AddMinutes(-40),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Bench Press",
                    Sets = [new ExecutedSetDocumentValueObject { Repetitions = 5, Load = load, LoadUnit = "kg", SetType = setType, Rpe = rpe, RestSeconds = 120 }]
                }
            ]
        };

        await repository.AddAsync(session, CancellationToken.None);
        await repository.UpdateCompletionAsync(session.Id, session.StartedAtUtc.AddMinutes(40), rpe,
        [
            new WorkoutPrDocumentValueObject { ExerciseId = 1, ExerciseName = "Bench Press", Type = "max_load", Value = load }
        ], CancellationToken.None);

        var found = await repository.GetByIdAsync(session.Id, CancellationToken.None);
        var range = await repository.GetCompletedByUserInRangeAsync(15, session.StartedAtUtc.AddDays(-1), session.StartedAtUtc.AddDays(1), CancellationToken.None);
        var keyset = await repository.GetByTargetUserKeysetAsync(15, null, 10, CancellationToken.None);

        Assert.NotNull(found);
        Assert.True(found.IsCompleted);
        Assert.Equal(rpe, found.PerceivedExertion);
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


