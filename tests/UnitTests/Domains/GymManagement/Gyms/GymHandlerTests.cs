using ShapeUp.Features.GymManagement.Gyms.CreateGym;
using ShapeUp.Features.GymManagement.Gyms.GetGyms;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.GymManagement.Gyms;

public class GymHandlerTests
{
    private readonly Mock<IGymRepository> _gymRepo = new();
    private readonly Mock<IPlatformTierRepository> _tierRepo = new();
    private readonly Mock<IUserPlatformRoleRepository> _roleRepo = new();

    public static IEnumerable<object[]> ValidGymCases =>
    [
        ["Alpha Gym", "Best gym", "Rua A, 1", null],
        ["Beta Fit", null, null, null],
        ["Mega Academy", "Large", "Av. B, 200", null],
    ];

    [Theory]
    [MemberData(nameof(ValidGymCases))]
    public async Task CreateGymHandler_ValidCommand_CreatesGymAndAssignsOwnerRole(string name, string? desc, string? address, int? tierId)
    {
        _gymRepo.Setup(r => r.AddAsync(It.IsAny<Gym>(), default))
                .Callback<Gym, CancellationToken>((g, _) => g.Id = 1)
                .Returns(Task.CompletedTask);
        _roleRepo.Setup(r => r.GetByUserIdAndRoleAsync(10, PlatformRoleType.GymOwner, default)).ReturnsAsync((UserPlatformRole?)null);
        _roleRepo.Setup(r => r.AddAsync(It.IsAny<UserPlatformRole>(), default)).Returns(Task.CompletedTask);

        var handler = new CreateGymHandler(_gymRepo.Object, _tierRepo.Object, _roleRepo.Object, new CreateGymValidator());
        var result = await handler.HandleAsync(new CreateGymCommand(name, desc, address, tierId), 10, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value!.Name);
        Assert.Equal(10, result.Value.OwnerId);
        _roleRepo.Verify(r => r.AddAsync(It.Is<UserPlatformRole>(x => x.UserId == 10 && x.Role == PlatformRoleType.GymOwner), default), Times.Once);
    }

    public static IEnumerable<object[]> InvalidGymNameCases =>
    [
        [""],
        [new string('a', 201)],
    ];

    [Theory]
    [MemberData(nameof(InvalidGymNameCases))]
    public async Task CreateGymHandler_InvalidName_ReturnsValidationFailure(string name)
    {
        var handler = new CreateGymHandler(_gymRepo.Object, _tierRepo.Object, _roleRepo.Object, new CreateGymValidator());
        var result = await handler.HandleAsync(new CreateGymCommand(name, null, null, null), 1, default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Theory]
    [InlineData(5, null, null)]
    [InlineData(5, 99, null)]
    public async Task CreateGymHandler_TierNotFound_ReturnsNotFound(int currentUserId, int? tierId, object? _)
    {
        if (tierId.HasValue)
            _tierRepo.Setup(r => r.GetByIdAsync(tierId.Value, default)).ReturnsAsync((PlatformTier?)null);

        if (!tierId.HasValue) return; // skip, only test with tier

        var handler = new CreateGymHandler(_gymRepo.Object, _tierRepo.Object, _roleRepo.Object, new CreateGymValidator());
        var result = await handler.HandleAsync(new CreateGymCommand("X", null, null, tierId), currentUserId, default);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetGymsHandler_ReturnsPagedResult(int pageSize)
    {
        var gyms = Enumerable.Range(1, pageSize)
            .Select(i => new Gym { Id = i, Name = $"Gym {i}", OwnerId = 1 })
            .ToList();
        _gymRepo.Setup(r => r.GetAllKeysetAsync(null, pageSize, default)).ReturnsAsync(gyms);

        var handler = new GetGymsHandler(_gymRepo.Object);
        var result = await handler.HandleAsync(new GetGymsQuery(null, pageSize, null), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(pageSize, result.Value!.Items.Length);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetGymsHandler_GetById_ReturnsGym(int gymId)
    {
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(new Gym { Id = gymId, Name = "Test", OwnerId = 1 });

        var handler = new GetGymsHandler(_gymRepo.Object);
        var result = await handler.GetByIdAsync(gymId, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(gymId, result.Value!.Id);
    }
}

