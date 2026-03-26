namespace IntegrationTests.Domains.GymManagement.Handlers;

using ShapeUp.Features.GymManagement.GymClients.EnrollGymClient;
using ShapeUp.Features.GymManagement.GymPlans.CreateGymPlan;
using ShapeUp.Features.GymManagement.GymStaff.AddGymStaff;
using ShapeUp.Features.GymManagement.Gyms.CreateGym;
using ShapeUp.Features.GymManagement.Infrastructure.Repositories;
using ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.GymManagement.TrainerClients.AddTrainerClient;
using ShapeUp.Features.GymManagement.TrainerPlans.CreateTrainerPlan;
using IntegrationTests.Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class GymManagementHandlerIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("Starter", "Entry plan", 29.90, null, null)]
    [InlineData("Pro", "Pro plan", 79.90, 50, 5)]
    public async Task CreatePlatformTierHandler_ShouldPersist(string name, string? desc, decimal price, int? maxClients, int? maxTrainers)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var handler = new CreatePlatformTierHandler(new PlatformTierRepository(ctx), new CreatePlatformTierValidator());

        var result = await handler.HandleAsync(new CreatePlatformTierCommand($"{name}-{Guid.NewGuid():N}", desc, PlatformRoleType.GymOwner, price, maxClients, maxTrainers), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Id > 0);
        Assert.Equal(PlatformRoleType.GymOwner, result.Value.TargetRole);
    }

    [Theory]
    [InlineData("Gym One", 1)]
    [InlineData("Gym Two", 2)]
    public async Task CreateGymHandler_ShouldCreateGymAndOwnerRole(string name, int ownerId)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var handler = new CreateGymHandler(
            new GymRepository(ctx),
            new PlatformTierRepository(ctx),
            new UserPlatformRoleRepository(ctx),
            new CreateGymValidator());

        var result = await handler.HandleAsync(new CreateGymCommand(name, null, null, null), ownerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ownerId, result.Value!.OwnerId);

        var roles = await new UserPlatformRoleRepository(ctx).GetByUserIdAsync(ownerId, CancellationToken.None);
        Assert.Contains(roles, r => r.Role == PlatformRoleType.GymOwner);
    }

    [Theory]
    [InlineData("Monthly Plan", 99.90, 30)]
    [InlineData("Annual Plan", 799.90, 365)]
    public async Task CreateGymPlanHandler_OwnerCreates_ShouldPersist(string planName, decimal price, int days)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var gym = new Gym { OwnerId = 1, Name = $"Gym-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);

        var staffRepo = new GymStaffRepository(ctx);
        var handler = new CreateGymPlanHandler(new GymPlanRepository(ctx), gymRepo, staffRepo, new CreateGymPlanValidator());

        var result = await handler.HandleAsync(new CreateGymPlanCommand(gym.Id, planName, null, price, days), gym.OwnerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(planName, result.Value!.Name);
    }

    [Theory]
    [InlineData(GymStaffRole.Trainer)]
    [InlineData(GymStaffRole.Receptionist)]
    public async Task AddGymStaffHandler_OwnerAdds_ShouldPersist(GymStaffRole role)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var gym = new Gym { OwnerId = 1, Name = $"Gym-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);

        var staffRepo = new GymStaffRepository(ctx);
        var handler = new AddGymStaffHandler(staffRepo, gymRepo, new AddGymStaffValidator());
        var result = await handler.HandleAsync(new AddGymStaffCommand(gym.Id, 99, role), gym.OwnerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(role.ToString(), result.Value!.Role);
    }

    [Theory]
    [InlineData(5, 30)]
    [InlineData(6, 60)]
    public async Task EnrollGymClientHandler_ShouldEnrollClient(int clientUserId, int planDays)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var gymRepo = new GymRepository(ctx);
        var planRepo = new GymPlanRepository(ctx);
        var staffRepo = new GymStaffRepository(ctx);
        var clientRepo = new GymClientRepository(ctx);

        var gym = new Gym { OwnerId = 1, Name = $"Gym-{Guid.NewGuid():N}" };
        await gymRepo.AddAsync(gym, CancellationToken.None);
        var plan = new GymPlan { GymId = gym.Id, Name = "P", Price = 50m, DurationDays = planDays };
        await planRepo.AddAsync(plan, CancellationToken.None);

        var handler = new EnrollGymClientHandler(clientRepo, gymRepo, planRepo, staffRepo, new EnrollGymClientValidator());
        var result = await handler.HandleAsync(new EnrollGymClientCommand(gym.Id, clientUserId, plan.Id, null), gym.OwnerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(clientUserId, result.Value!.UserId);
    }

    [Theory]
    [InlineData(100, 30)]
    [InlineData(101, 60)]
    public async Task CreateTrainerPlanHandler_ShouldPersist(int trainerId, int days)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var handler = new CreateTrainerPlanHandler(new TrainerPlanRepository(ctx), new CreateTrainerPlanValidator());
        var result = await handler.HandleAsync(new CreateTrainerPlanCommand($"Plan-{Guid.NewGuid():N}", null, 49.90m, days), trainerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(trainerId, result.Value!.TrainerId);
    }

    [Theory]
    [InlineData(200, 300)]
    [InlineData(201, 301)]
    public async Task AddTrainerClientHandler_ShouldRegisterClient(int trainerId, int clientId)
    {
        await using var ctx = fixture.CreateGymManagementDbContext();
        var planRepo = new TrainerPlanRepository(ctx);
        var clientRepo = new TrainerClientRepository(ctx);

        var plan = new TrainerPlan { TrainerId = trainerId, Name = $"P-{Guid.NewGuid():N}", Price = 50m, DurationDays = 30 };
        await planRepo.AddAsync(plan, CancellationToken.None);

        var handler = new AddTrainerClientHandler(clientRepo, planRepo, new AddTrainerClientValidator());
        var result = await handler.HandleAsync(new AddTrainerClientCommand(clientId, plan.Id), trainerId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(clientId, result.Value!.ClientId);
    }
}

