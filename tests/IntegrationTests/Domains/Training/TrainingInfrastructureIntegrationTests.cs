using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Training.Infrastructure.Data;
using ShapeUp.Features.Training.Infrastructure.Policies;
using ShapeUp.Features.Training.Infrastructure.Repositories;
using ShapeUp.Features.Training.Shared.Entities;
using Xunit;

namespace IntegrationTests;

public class TrainingInfrastructureIntegrationTests
{
    [Fact]
    public async Task ExerciseRepository_ShouldReturnKeysetPageInDescendingOrder()
    {
        await using var dbContext = CreateTrainingContext();
        dbContext.Exercises.AddRange(
            new Exercise { Name = "Bench Press", NamePt = "Supino" },
            new Exercise { Name = "Squat", NamePt = "Agachamento" },
            new Exercise { Name = "Deadlift", NamePt = "Levantamento Terra" });
        await dbContext.SaveChangesAsync();

        var repository = new ExerciseRepository(dbContext);

        var page1 = await repository.GetKeysetPageAsync(null, 2, CancellationToken.None);
        var page2 = await repository.GetKeysetPageAsync(page1.Last().Id, 2, CancellationToken.None);

        page1.Should().HaveCount(2);
        page1[0].Id.Should().BeGreaterThan(page1[1].Id);
        page2.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExerciseRepository_SuggestShouldFilterByNameMusclesAndEquipment()
    {
        await using var dbContext = CreateTrainingContext();

        var barbell = new Equipment { Name = "Barbell", NamePt = "Barra" };
        var dumbbell = new Equipment { Name = "Dumbbell", NamePt = "Haltere" };
        dbContext.Equipments.AddRange(barbell, dumbbell);

        var chest = new Muscle { Name = "Chest", NamePt = "Peito" };
        var biceps = new Muscle { Name = "Biceps", NamePt = "Bíceps" };
        dbContext.Muscles.AddRange(chest, biceps);

        await dbContext.SaveChangesAsync();

        var benchPress = new Exercise
        {
            Name = "Bench Press",
            NamePt = "Supino",
            MuscleProfiles = [new ExerciseMuscleProfile { MuscleId = chest.Id, ActivationPercent = 70 }],
            ExerciseEquipments = [new ExerciseEquipment { EquipmentId = barbell.Id }]
        };

        var bicepsCurl = new Exercise
        {
            Name = "Biceps Curl",
            NamePt = "Rosca Direta",
            MuscleProfiles = [new ExerciseMuscleProfile { MuscleId = biceps.Id, ActivationPercent = 80 }],
            ExerciseEquipments = [new ExerciseEquipment { EquipmentId = dumbbell.Id }]
        };

        dbContext.Exercises.AddRange(benchPress, bicepsCurl);
        await dbContext.SaveChangesAsync();

        var repository = new ExerciseRepository(dbContext);
        var suggestions = await repository.SuggestAsync("press", [chest.Id], [barbell.Id], 10, CancellationToken.None);

        suggestions.Should().ContainSingle();
        suggestions[0].Name.Should().Be("Bench Press");
    }

    [Fact]
    public async Task TrainingAccessPolicy_ShouldAllowGymTrainerStaffToCreateForGymClient()
    {
        await using var gymContext = CreateGymContext();

        gymContext.GymStaff.Add(new GymStaff
        {
            GymId = 1,
            UserId = 100,
            Role = GymStaffRole.Trainer,
            IsActive = true
        });

        gymContext.GymClients.Add(new GymClient
        {
            GymId = 1,
            UserId = 200,
            GymPlanId = 1,
            IsActive = true
        });

        await gymContext.SaveChangesAsync();

        var policy = new TrainingAccessPolicy(gymContext);
        var allowed = await policy.CanCreateWorkoutForAsync(100, 200, ["training:workouts:create:gym_staff"], CancellationToken.None);

        allowed.Should().BeTrue();
    }

    private static TrainingDbContext CreateTrainingContext()
    {
        var options = new DbContextOptionsBuilder<TrainingDbContext>()
            .UseInMemoryDatabase($"training-tests-{Guid.NewGuid()}")
            .Options;

        return new TrainingDbContext(options);
    }

    private static GymManagementDbContext CreateGymContext()
    {
        var options = new DbContextOptionsBuilder<GymManagementDbContext>()
            .UseInMemoryDatabase($"gym-tests-{Guid.NewGuid()}")
            .Options;

        var context = new GymManagementDbContext(options);

        context.Gyms.Add(new Gym { Name = "Gym 1", OwnerId = 1, PlatformTierId = null });
        context.GymPlans.Add(new GymPlan { GymId = 1, Name = "Basic", Price = 100 });
        context.SaveChanges();

        return context;
    }
}
