namespace IntegrationTests.Domains.GymManagement.Repositories;

using ShapeUp.Features.GymManagement.Infrastructure.Repositories;
using ShapeUp.Features.GymManagement.Shared.Entities;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class GymManagementRepositoryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("Basic Plan", 29.90, null, null)]
    [InlineData("Pro Plan", 79.90, 50, 5)]
    [InlineData("Enterprise", 199.90, 500, 50)]
    public async Task PlatformTierRepository_AddAndGetById_ShouldPersist(string name, decimal price, int? maxClients, int? maxTrainers)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new PlatformTierRepository(ctx);

        var tier = new PlatformTier { Name = $"{name}-{Guid.NewGuid():N}", Price = price, MaxClients = maxClients, MaxTrainers = maxTrainers };
        await repo.AddAsync(tier, CancellationToken.None);

        var found = await repo.GetByIdAsync(tier.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(tier.Name, found.Name);
        Assert.Equal(price, found.Price);
    }

    [Theory]
    [InlineData("Gym Alpha", null)]
    [InlineData("Gym Beta", "Located at Main St")]
    public async Task GymRepository_AddAndGetById_ShouldPersist(string name, string? desc)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new GymRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = name, Description = desc };
        await repo.AddAsync(gym, CancellationToken.None);

        var found = await repo.GetByIdAsync(gym.Id, CancellationToken.None);
        Assert.NotNull(found);
        Assert.Equal(name, found.Name);
        Assert.Equal(1, found.OwnerId);
    }

    [Theory]
    [InlineData("Gym Alpha", "Original desc", "Updated Alpha", "Updated desc")]
    [InlineData("Gym Beta", null, "New Beta", "Now with description")]
    public async Task GymRepository_Update_ShouldPersistChanges(string originalName, string? originalDesc, string newName, string? newDesc)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new GymRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = originalName, Description = originalDesc, Address = "Street 1" };
        await repo.AddAsync(gym, CancellationToken.None);

        gym.Name = newName;
        gym.Description = newDesc;
        gym.Address = "New Street 2";
        await repo.UpdateAsync(gym, CancellationToken.None);

        var updated = await repo.GetByIdAsync(gym.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(newName, updated.Name);
        Assert.Equal(newDesc, updated.Description);
        Assert.Equal("New Street 2", updated.Address);
    }

    [Fact]
    public async Task GymRepository_Delete_ShouldRemoveGym()
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new GymRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = "To Delete" };
        await repo.AddAsync(gym, CancellationToken.None);
        var gymId = gym.Id;

        await repo.DeleteAsync(gymId, CancellationToken.None);

        var deleted = await repo.GetByIdAsync(gymId, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GymRepository_GetAllKeyset_ShouldReturnAllGyms(int pageSize)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new GymRepository(ctx);

        var gym1 = new Gym { OwnerId = 1, Name = $"Gym1-{Guid.NewGuid():N}" };
        var gym2 = new Gym { OwnerId = 2, Name = $"Gym2-{Guid.NewGuid():N}" };
        var gym3 = new Gym { OwnerId = 3, Name = $"Gym3-{Guid.NewGuid():N}" };
        
        await repo.AddAsync(gym1, CancellationToken.None);
        await repo.AddAsync(gym2, CancellationToken.None);
        await repo.AddAsync(gym3, CancellationToken.None);

        var result = await repo.GetAllKeysetAsync(null, pageSize, CancellationToken.None);
        Assert.NotEmpty(result);
        Assert.True(result.Count <= pageSize);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GymPlanRepository_AddAndList_ShouldReturnByGym(int gymId)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var planRepo = new GymPlanRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = $"Gym-{gymId}-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);

        var plan = new GymPlan { GymId = gym.Id, Name = "Monthly", Price = 99m, DurationDays = 30 };
        await planRepo.AddAsync(plan, CancellationToken.None);

        var plans = await planRepo.GetByGymIdKeysetAsync(gym.Id, null, 10, CancellationToken.None);
        Assert.Single(plans);
        Assert.Equal("Monthly", plans[0].Name);
    }

    [Theory]
    [InlineData("Plan Alpha", 49.90)]
    [InlineData("Plan Beta", 99.90)]
    public async Task GymPlanRepository_Update_ShouldPersistChanges(string newName, decimal newPrice)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var planRepo = new GymPlanRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = $"Gym-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);

        var plan = new GymPlan { GymId = gym.Id, Name = "Original", Price = 29.90m, DurationDays = 30 };
        await planRepo.AddAsync(plan, CancellationToken.None);

        plan.Name = newName;
        plan.Price = newPrice;
        await planRepo.UpdateAsync(plan, CancellationToken.None);

        var updated = await planRepo.GetByIdAsync(plan.Id, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(newName, updated.Name);
        Assert.Equal(newPrice, updated.Price);
    }

    [Fact]
    public async Task GymPlanRepository_Delete_ShouldRemovePlan()
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var planRepo = new GymPlanRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = $"Gym-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);

        var plan = new GymPlan { GymId = gym.Id, Name = "To Delete", Price = 29.90m, DurationDays = 30 };
        await planRepo.AddAsync(plan, CancellationToken.None);
        var planId = plan.Id;

        await planRepo.DeleteAsync(planId, CancellationToken.None);

        var deleted = await planRepo.GetByIdAsync(planId, CancellationToken.None);
        Assert.Null(deleted);
    }

    [Theory]
    [InlineData(GymStaffRole.Trainer)]
    [InlineData(GymStaffRole.Receptionist)]
    public async Task GymStaffRepository_AddAndIsStaff_ShouldReturnTrue(GymStaffRole role)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var staffRepo = new GymStaffRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = $"StaffGym-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);

        var staff = new GymStaff { GymId = gym.Id, UserId = 50, Role = role };
        await staffRepo.AddAsync(staff, CancellationToken.None);

        var isStaff = await staffRepo.IsStaffAsync(gym.Id, 50, CancellationToken.None);
        Assert.True(isStaff);
    }

    [Fact]
    public async Task GymStaffRepository_Delete_ShouldRemoveStaff()
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var staffRepo = new GymStaffRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = $"StaffGym-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);

        var staff = new GymStaff { GymId = gym.Id, UserId = 50, Role = GymStaffRole.Trainer };
        await staffRepo.AddAsync(staff, CancellationToken.None);
        var staffId = staff.Id;

        await staffRepo.RemoveAsync(staffId, CancellationToken.None);

        var isStaff = await staffRepo.IsStaffAsync(gym.Id, 50, CancellationToken.None);
        Assert.False(isStaff);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task TrainerPlanRepository_AddAndList_ShouldReturnByTrainer(int trainerId)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var repo = new TrainerPlanRepository(ctx);

        var plan = new TrainerPlan { TrainerId = trainerId, Name = $"Plan-{Guid.NewGuid():N}", Price = 50m, DurationDays = 30 };
        await repo.AddAsync(plan, CancellationToken.None);

        var plans = await repo.GetByTrainerIdKeysetAsync(trainerId, null, 10, CancellationToken.None);
        Assert.Contains(plans, p => p.Id == plan.Id);
    }

    [Theory]
    [InlineData(10, 20)]
    [InlineData(11, 21)]
    public async Task TrainerClientRepository_AddAndTransfer_ShouldUpdateTrainer(int trainerId1, int trainerId2)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var planRepo = new TrainerPlanRepository(ctx);
        var clientRepo = new TrainerClientRepository(ctx);

        var plan1 = new TrainerPlan { TrainerId = trainerId1, Name = $"P1-{Guid.NewGuid():N}", Price = 50m, DurationDays = 30 };
        var plan2 = new TrainerPlan { TrainerId = trainerId2, Name = $"P2-{Guid.NewGuid():N}", Price = 60m, DurationDays = 30 };
        await planRepo.AddAsync(plan1, CancellationToken.None);
        await planRepo.AddAsync(plan2, CancellationToken.None);

        var client = new TrainerClient { TrainerId = trainerId1, ClientId = 99, TrainerPlanId = plan1.Id };
        await clientRepo.AddAsync(client, CancellationToken.None);

        await clientRepo.TransferAsync(client.Id, trainerId2, plan2.Id, CancellationToken.None);

        var transferred = await clientRepo.GetByIdAsync(client.Id, CancellationToken.None);
        Assert.Equal(trainerId2, transferred!.TrainerId);
        Assert.Equal(plan2.Id, transferred.TrainerPlanId);
    }
}

