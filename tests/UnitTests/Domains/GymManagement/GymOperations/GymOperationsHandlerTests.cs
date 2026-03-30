using ShapeUp.Features.GymManagement.GymClients.AssignClientTrainer;
using ShapeUp.Features.GymManagement.GymClients.EnrollGymClient;
using ShapeUp.Features.GymManagement.GymPlans.CreateGymPlan;
using ShapeUp.Features.GymManagement.GymStaff.AddGymStaff;
using ShapeUp.Features.GymManagement.GymStaff.RemoveGymStaff;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.GymManagement.GymOperations;

public class GymOperationsHandlerTests
{
    private readonly Mock<IGymRepository> _gymRepo = new();
    private readonly Mock<IGymPlanRepository> _planRepo = new();
    private readonly Mock<IGymStaffRepository> _staffRepo = new();
    private readonly Mock<IGymClientRepository> _clientRepo = new();
    private readonly Mock<ITrainerClientRepository> _trainerClientRepo = new();
    private readonly Mock<IUserPlatformRoleRepository> _roleRepo = new();

    private Gym SeedGym(int gymId = 1, int ownerId = 1) => new Gym { Id = gymId, OwnerId = ownerId, Name = "Test Gym" };

    public static IEnumerable<object[]> CreatePlanCases =>
    [
        [1, 1, "Monthly", 99.90, 30],
        [1, 1, "Annual", 799.90, 365],
        [2, 2, "Basic", 49.90, 30],
    ];

    [Theory]
    [MemberData(nameof(CreatePlanCases))]
    public async Task CreateGymPlanHandler_OwnerCanCreate_ReturnsSuccess(int gymId, int ownerId, string name, decimal price, int days)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, ownerId, ownerId, default)).ReturnsAsync(true);
        _planRepo.Setup(r => r.AddAsync(It.IsAny<GymPlan>(), default))
                 .Callback<GymPlan, CancellationToken>((p, _) => p.Id = 1)
                 .Returns(Task.CompletedTask);

        var handler = new CreateGymPlanHandler(_planRepo.Object, _gymRepo.Object, _staffRepo.Object, new CreateGymPlanValidator());
        var result = await handler.HandleAsync(new CreateGymPlanCommand(gymId, name, null, price, days), ownerId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value!.Name);
    }

    [Theory]
    [InlineData(1, 999)]
    [InlineData(2, 888)]
    public async Task CreateGymPlanHandler_NonOwner_ReturnsForbidden(int gymId, int nonOwnerId)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId: 1));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, nonOwnerId, 1, default)).ReturnsAsync(false);

        var handler = new CreateGymPlanHandler(_planRepo.Object, _gymRepo.Object, _staffRepo.Object, new CreateGymPlanValidator());
        var result = await handler.HandleAsync(new CreateGymPlanCommand(gymId, "Plan", null, 10m, 30), nonOwnerId, default);

        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    public static IEnumerable<object[]> AddStaffCases =>
    [
        [1, 1, 20, GymStaffRole.Trainer],
        [1, 1, 21, GymStaffRole.Receptionist],
        [2, 2, 30, GymStaffRole.Trainer],
    ];

    [Theory]
    [MemberData(nameof(AddStaffCases))]
    public async Task AddGymStaffHandler_OwnerAddsStaff_ReturnsSuccess(int gymId, int ownerId, int newUserId, GymStaffRole role)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, ownerId, ownerId, default)).ReturnsAsync(true);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, newUserId, default)).ReturnsAsync(false);
        _staffRepo.Setup(r => r.AddAsync(It.IsAny<GymStaff>(), default))
                  .Callback<GymStaff, CancellationToken>((s, _) => s.Id = 1)
                  .Returns(Task.CompletedTask);

        var handler = new AddGymStaffHandler(_staffRepo.Object, _gymRepo.Object, new AddGymStaffValidator());
        var result = await handler.HandleAsync(new AddGymStaffCommand(gymId, newUserId, role), ownerId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(role.ToString(), result.Value!.Role);
    }

    [Theory]
    [InlineData(1, 1, 20)]
    [InlineData(2, 2, 30)]
    public async Task AddGymStaffHandler_UserAlreadyStaff_ReturnsConflict(int gymId, int ownerId, int existingUserId)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, ownerId, ownerId, default)).ReturnsAsync(true);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, existingUserId, default)).ReturnsAsync(true);

        var handler = new AddGymStaffHandler(_staffRepo.Object, _gymRepo.Object, new AddGymStaffValidator());
        var result = await handler.HandleAsync(new AddGymStaffCommand(gymId, existingUserId, GymStaffRole.Trainer), ownerId, default);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
    }

    public static IEnumerable<object[]> EnrollClientCases =>
    [
        [1, 1, 10, 1, null],
        [2, 2, 11, 2, null],
    ];

    [Theory]
    [MemberData(nameof(EnrollClientCases))]
    public async Task EnrollGymClientHandler_ValidEnroll_ReturnsSuccess(int gymId, int ownerId, int clientUserId, int planId, int? trainerId)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, ownerId, ownerId, default)).ReturnsAsync(true);
        _planRepo.Setup(r => r.GetByIdAsync(planId, default)).ReturnsAsync(new GymPlan { Id = planId, GymId = gymId, Name = "P" });
        _clientRepo.Setup(r => r.GetByGymAndUserAsync(gymId, clientUserId, default)).ReturnsAsync((GymClient?)null);
        _trainerClientRepo.Setup(r => r.GetByClientIdAsync(clientUserId, default)).ReturnsAsync((TrainerClient?)null);
        _clientRepo.Setup(r => r.AddAsync(It.IsAny<GymClient>(), default))
                   .Callback<GymClient, CancellationToken>((c, _) => c.Id = 1)
                   .Returns(Task.CompletedTask);
        _roleRepo.Setup(r => r.GetByUserIdAndRoleAsync(clientUserId, It.IsAny<PlatformRoleType>(), default))
            .ReturnsAsync((UserPlatformRole?)null);
        _roleRepo.Setup(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default)).Returns(Task.CompletedTask);
        _roleRepo.Setup(r => r.DeleteAsync(It.IsAny<int>(), default)).Returns(Task.CompletedTask);

        var handler = new EnrollGymClientHandler(
            _clientRepo.Object,
            _trainerClientRepo.Object,
            _gymRepo.Object,
            _planRepo.Object,
            _staffRepo.Object,
            _roleRepo.Object,
            new EnrollGymClientValidator());
        var result = await handler.HandleAsync(new EnrollGymClientCommand(gymId, clientUserId, planId, trainerId), ownerId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(clientUserId, result.Value!.UserId);
    }

    [Theory]
    [InlineData(1, 1, 5, 99)]
    [InlineData(2, 2, 6, 88)]
    public async Task EnrollGymClientHandler_AlreadyEnrolled_ReturnsConflict(int gymId, int ownerId, int clientUserId, int planId)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, ownerId, ownerId, default)).ReturnsAsync(true);
        _planRepo.Setup(r => r.GetByIdAsync(planId, default)).ReturnsAsync(new GymPlan { Id = planId, GymId = gymId, Name = "P" });
        _clientRepo.Setup(r => r.GetByGymAndUserAsync(gymId, clientUserId, default))
                   .ReturnsAsync(new GymClient { Id = 99, GymId = gymId, UserId = clientUserId, GymPlanId = planId });

        var handler = new EnrollGymClientHandler(
            _clientRepo.Object,
            _trainerClientRepo.Object,
            _gymRepo.Object,
            _planRepo.Object,
            _staffRepo.Object,
            _roleRepo.Object,
            new EnrollGymClientValidator());
        var result = await handler.HandleAsync(new EnrollGymClientCommand(gymId, clientUserId, planId, null), ownerId, default);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
    }

    [Theory]
    [InlineData(1, 1, 10, 5)]
    [InlineData(2, 2, 11, 6)]
    public async Task AssignClientTrainerHandler_ValidAssign_ReturnsSuccess(int gymId, int ownerId, int clientId, int trainerId)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, ownerId, ownerId, default)).ReturnsAsync(true);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, ownerId, default)).ReturnsAsync(true);
        _clientRepo.Setup(r => r.GetByIdAsync(clientId, default))
                   .ReturnsAsync(new GymClient { Id = clientId, GymId = gymId, UserId = 99, GymPlanId = 1 });
        _staffRepo.Setup(r => r.GetByIdAsync(trainerId, default))
                  .ReturnsAsync(new GymStaff { Id = trainerId, GymId = gymId, UserId = 77, Role = GymStaffRole.Trainer });
        _clientRepo.Setup(r => r.AssignTrainerAsync(clientId, trainerId, default)).Returns(Task.CompletedTask);

        var handler = new AssignClientTrainerHandler(_clientRepo.Object, _gymRepo.Object, _staffRepo.Object);
        var result = await handler.HandleAsync(new AssignClientTrainerCommand(gymId, clientId, trainerId), ownerId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(trainerId, result.Value!.TrainerId);
    }

    [Theory]
    [InlineData(1, 1, 50)]
    [InlineData(2, 2, 51)]
    public async Task RemoveGymStaffHandler_OwnerRemovesStaff_ReturnsSuccess(int gymId, int ownerId, int staffId)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(SeedGym(gymId, ownerId));
        _staffRepo.Setup(r => r.IsOwnerOrReceptionistAsync(gymId, ownerId, ownerId, default)).ReturnsAsync(true);
        _staffRepo.Setup(r => r.GetByIdAsync(staffId, default))
                  .ReturnsAsync(new GymStaff { Id = staffId, GymId = gymId, UserId = 99, Role = GymStaffRole.Trainer });
        _staffRepo.Setup(r => r.RemoveAsync(staffId, default)).Returns(Task.CompletedTask);

        var handler = new RemoveGymStaffHandler(_staffRepo.Object, _gymRepo.Object);
        var result = await handler.HandleAsync(new RemoveGymStaffCommand(gymId, staffId), ownerId, default);

        Assert.True(result.IsSuccess);
        _staffRepo.Verify(r => r.RemoveAsync(staffId, default), Times.Once);
    }
}

