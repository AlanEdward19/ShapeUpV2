namespace UnitTests.Features.GymManagement.PlatformTiers;

using Moq;
using ShapeUp.Features.GymManagement.PlatformTiers.CreatePlatformTier;
using ShapeUp.Features.GymManagement.PlatformTiers.DeletePlatformTier;
using ShapeUp.Features.GymManagement.PlatformTiers.GetPlatformTiers;
using ShapeUp.Features.GymManagement.PlatformTiers.UpdatePlatformTier;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

public class PlatformTierHandlerTests
{
    private readonly Mock<IPlatformTierRepository> _repo = new();

    public static IEnumerable<object[]> ValidCreateCases =>
    [
        ["Basic",      "Desc",     PlatformRoleType.Client,   29.90,  null, null],
        ["Pro",        "Pro plan", PlatformRoleType.Trainer,  79.90,  50,   5],
        ["Enterprise", null,       PlatformRoleType.GymOwner, 199.90, 500,  50],
    ];

    [Theory]
    [MemberData(nameof(ValidCreateCases))]
    public async Task CreatePlatformTierHandler_ValidCommand_ReturnsSuccess(
        string name, string? desc, PlatformRoleType targetRole, decimal price, int? maxClients, int? maxTrainers)
    {
        _repo.Setup(r => r.GetByNameAsync(name, default)).ReturnsAsync((PlatformTier?)null);
        _repo.Setup(r => r.AddAsync(It.IsAny<PlatformTier>(), default)).Returns(Task.CompletedTask)
             .Callback<PlatformTier, CancellationToken>((t, _) => t.Id = 1);

        var handler = new CreatePlatformTierHandler(_repo.Object, new CreatePlatformTierValidator());
        var result = await handler.HandleAsync(new CreatePlatformTierCommand(name, desc, targetRole, price, maxClients, maxTrainers), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value!.Name);
        Assert.Equal(targetRole, result.Value.TargetRole);
        Assert.Equal(price, result.Value.Price);
    }

    public static IEnumerable<object[]> InvalidCreateCases =>
    [
        ["",               "desc", PlatformRoleType.Client,   10m,  null, null],
        [new string('x', 101), "desc", PlatformRoleType.Client, 10m, null, null],
        ["ValidName",      "desc", PlatformRoleType.Client,  -1m,  null, null],
        ["ValidName",      "desc", PlatformRoleType.Trainer,  10m,  -5,  null],
    ];

    [Theory]
    [MemberData(nameof(InvalidCreateCases))]
    public async Task CreatePlatformTierHandler_InvalidCommand_ReturnsFailure(
        string name, string? desc, PlatformRoleType targetRole, decimal price, int? maxClients, int? maxTrainers)
    {
        var handler = new CreatePlatformTierHandler(_repo.Object, new CreatePlatformTierValidator());
        var result = await handler.HandleAsync(new CreatePlatformTierCommand(name, desc, targetRole, price, maxClients, maxTrainers), default);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Theory]
    [InlineData("Duplicate Plan")]
    [InlineData("Another Dup")]
    public async Task CreatePlatformTierHandler_DuplicateName_ReturnsConflict(string name)
    {
        _repo.Setup(r => r.GetByNameAsync(name, default)).ReturnsAsync(new PlatformTier { Name = name });

        var handler = new CreatePlatformTierHandler(_repo.Object, new CreatePlatformTierValidator());
        var result = await handler.HandleAsync(new CreatePlatformTierCommand(name, null, PlatformRoleType.Client, 10m, null, null), default);

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error!.Code);
    }

    public static IEnumerable<object[]> UpdateCases =>
    [
        [1, "Updated", PlatformRoleType.GymOwner, 49.90, true],
        [2, "New Name", PlatformRoleType.Trainer,  0m,   false],
    ];

    [Theory]
    [MemberData(nameof(UpdateCases))]
    public async Task UpdatePlatformTierHandler_ExistingTier_ReturnsSuccess(
        int id, string name, PlatformRoleType targetRole, decimal price, bool isActive)
    {
        var existing = new PlatformTier { Id = id, Name = "Old", Price = 10m, TargetRole = PlatformRoleType.Client };
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(existing);
        _repo.Setup(r => r.UpdateAsync(It.IsAny<PlatformTier>(), default)).Returns(Task.CompletedTask);

        var handler = new UpdatePlatformTierHandler(_repo.Object, new UpdatePlatformTierValidator());
        var result = await handler.HandleAsync(new UpdatePlatformTierCommand(id, name, null, targetRole, price, null, null, isActive), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value!.Name);
        Assert.Equal(targetRole, result.Value.TargetRole);
        Assert.Equal(isActive, result.Value.IsActive);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(100)]
    public async Task UpdatePlatformTierHandler_NotFound_ReturnsNotFound(int id)
    {
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((PlatformTier?)null);

        var handler = new UpdatePlatformTierHandler(_repo.Object, new UpdatePlatformTierValidator());
        var result = await handler.HandleAsync(new UpdatePlatformTierCommand(id, "X", null, PlatformRoleType.Client, 10m, null, null, true), default);

        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task DeletePlatformTierHandler_ExistingTier_ReturnsSuccess(int id)
    {
        _repo.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync(new PlatformTier { Id = id, Name = "X" });
        _repo.Setup(r => r.DeleteAsync(id, default)).Returns(Task.CompletedTask);

        var handler = new DeletePlatformTierHandler(_repo.Object);
        var result = await handler.HandleAsync(new DeletePlatformTierCommand(id), default);

        Assert.True(result.IsSuccess);
        _repo.Verify(r => r.DeleteAsync(id, default), Times.Once);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetPlatformTiersHandler_ReturnsPagedResult(int pageSize)
    {
        var tiers = Enumerable.Range(1, pageSize + 1)
            .Select(i => new PlatformTier { Id = i, Name = $"Tier {i}", Price = i * 10m, TargetRole = PlatformRoleType.Client })
            .ToList();
        _repo.Setup(r => r.GetAllKeysetAsync(null, pageSize, default)).ReturnsAsync(tiers.Take(pageSize).ToList());

        var handler = new GetPlatformTiersHandler(_repo.Object);
        var result = await handler.HandleAsync(new GetPlatformTiersQuery(null, pageSize), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(pageSize, result.Value!.Items.Length);
    }
}

