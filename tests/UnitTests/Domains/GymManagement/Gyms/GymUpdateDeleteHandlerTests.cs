using Moq;
using ShapeUp.Features.GymManagement.Gyms.UpdateGym;
using ShapeUp.Features.GymManagement.Gyms.DeleteGym;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;

namespace UnitTests.Domains.GymManagement.Gyms;

public class GymUpdateDeleteHandlerTests
{
    private readonly Mock<IGymRepository> _gymRepo = new();
    private readonly Mock<IPlatformTierRepository> _tierRepo = new();
    private readonly Mock<IGymStaffRepository> _staffRepo = new();

    [Theory]
    [InlineData("Updated Gym", "Updated description", "New Address", null, true)]
    [InlineData("Gym 2", null, null, null, false)]
    public async Task UpdateGymHandler_ValidCommand_UpdatesGym(string name, string? desc, string? address, int? tierId, bool isActive)
    {
        // Arrange
        var gymId = 1;
        var ownerId = 10;
        var gym = new Gym
        {
            Id = gymId,
            OwnerId = ownerId,
            Name = "Old Name",
            Description = "Old desc",
            Address = "Old address",
            IsActive = true
        };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, ownerId, default)).ReturnsAsync(false);
        _gymRepo.Setup(r => r.UpdateAsync(It.IsAny<Gym>(), default)).Returns(Task.CompletedTask);

        var handler = new UpdateGymHandler(_gymRepo.Object, _tierRepo.Object, _staffRepo.Object, new UpdateGymValidator());

        // Act
        var result = await handler.HandleAsync(
            new UpdateGymCommand(gymId, name, desc, address, tierId, isActive),
            ownerId,
            default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(name, result.Value!.Name);
        Assert.Equal(ownerId, result.Value.OwnerId);
        _gymRepo.Verify(r => r.UpdateAsync(It.Is<Gym>(g => g.Name == name), default), Times.Once);
    }

    [Fact]
    public async Task UpdateGymHandler_GymNotFound_ReturnsNotFound()
    {
        // Arrange
        var gymId = 999;
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync((Gym?)null);

        var handler = new UpdateGymHandler(_gymRepo.Object, _tierRepo.Object, _staffRepo.Object, new UpdateGymValidator());

        // Act
        var result = await handler.HandleAsync(
            new UpdateGymCommand(gymId, "Name", null, null, null, true),
            10,
            default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateGymHandler_NotOwnerOrStaff_ReturnsForbidden()
    {
        // Arrange
        var gymId = 1;
        var ownerId = 10;
        var currentUserId = 20;
        var gym = new Gym { Id = gymId, OwnerId = ownerId, Name = "Test" };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, currentUserId, default)).ReturnsAsync(false);

        var handler = new UpdateGymHandler(_gymRepo.Object, _tierRepo.Object, _staffRepo.Object, new UpdateGymValidator());

        // Act
        var result = await handler.HandleAsync(
            new UpdateGymCommand(gymId, "Name", null, null, null, true),
            currentUserId,
            default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateGymHandler_TierNotFound_ReturnsNotFound()
    {
        // Arrange
        var gymId = 1;
        var ownerId = 10;
        var tierId = 99;
        var gym = new Gym { Id = gymId, OwnerId = ownerId, Name = "Test" };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, ownerId, default)).ReturnsAsync(false);
        _tierRepo.Setup(r => r.GetByIdAsync(tierId, default)).ReturnsAsync((PlatformTier?)null);

        var handler = new UpdateGymHandler(_gymRepo.Object, _tierRepo.Object, _staffRepo.Object, new UpdateGymValidator());

        // Act
        var result = await handler.HandleAsync(
            new UpdateGymCommand(gymId, "Name", null, null, tierId, true),
            ownerId,
            default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task UpdateGymHandler_InvalidName_ReturnsValidationFailure(string name)
    {
        // Arrange
        var gymId = 1;
        var gym = new Gym { Id = gymId, OwnerId = 10, Name = "Test" };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, 10, default)).ReturnsAsync(false);

        var handler = new UpdateGymHandler(_gymRepo.Object, _tierRepo.Object, _staffRepo.Object, new UpdateGymValidator());

        // Act
        var result = await handler.HandleAsync(
            new UpdateGymCommand(gymId, name, null, null, null, true),
            10,
            default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }

    [Fact]
    public async Task UpdateGymHandler_StaffCanUpdate_ReturnsSuccess()
    {
        // Arrange
        var gymId = 1;
        var ownerId = 10;
        var staffUserId = 15;
        var gym = new Gym
        {
            Id = gymId,
            OwnerId = ownerId,
            Name = "Old Name",
            IsActive = true
        };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, staffUserId, default)).ReturnsAsync(true);
        _gymRepo.Setup(r => r.UpdateAsync(It.IsAny<Gym>(), default)).Returns(Task.CompletedTask);

        var handler = new UpdateGymHandler(_gymRepo.Object, _tierRepo.Object, _staffRepo.Object, new UpdateGymValidator());

        // Act
        var result = await handler.HandleAsync(
            new UpdateGymCommand(gymId, "Updated Name", null, null, null, true),
            staffUserId,
            default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value!.Name);
    }

    // Delete Handler Tests

    [Fact]
    public async Task DeleteGymHandler_ValidCommand_DeletesGym()
    {
        // Arrange
        var gymId = 1;
        var ownerId = 10;
        var gym = new Gym { Id = gymId, OwnerId = ownerId, Name = "Test Gym" };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, ownerId, default)).ReturnsAsync(false);
        _gymRepo.Setup(r => r.DeleteAsync(gymId, default)).Returns(Task.CompletedTask);

        var handler = new DeleteGymHandler(_gymRepo.Object, _staffRepo.Object);

        // Act
        var result = await handler.HandleAsync(new DeleteGymCommand(gymId), ownerId, default);

        // Assert
        Assert.True(result.IsSuccess);
        _gymRepo.Verify(r => r.DeleteAsync(gymId, default), Times.Once);
    }

    [Fact]
    public async Task DeleteGymHandler_GymNotFound_ReturnsNotFound()
    {
        // Arrange
        var gymId = 999;
        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync((Gym?)null);

        var handler = new DeleteGymHandler(_gymRepo.Object, _staffRepo.Object);

        // Act
        var result = await handler.HandleAsync(new DeleteGymCommand(gymId), 10, default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("not_found", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteGymHandler_NotOwnerOrStaff_ReturnsForbidden()
    {
        // Arrange
        var gymId = 1;
        var ownerId = 10;
        var currentUserId = 20;
        var gym = new Gym { Id = gymId, OwnerId = ownerId, Name = "Test Gym" };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, currentUserId, default)).ReturnsAsync(false);

        var handler = new DeleteGymHandler(_gymRepo.Object, _staffRepo.Object);

        // Act
        var result = await handler.HandleAsync(new DeleteGymCommand(gymId), currentUserId, default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("forbidden", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteGymHandler_StaffCanDelete_ReturnsSuccess()
    {
        // Arrange
        var gymId = 1;
        var ownerId = 10;
        var staffUserId = 15;
        var gym = new Gym { Id = gymId, OwnerId = ownerId, Name = "Test Gym" };

        _gymRepo.Setup(r => r.GetByIdAsync(gymId, default)).ReturnsAsync(gym);
        _staffRepo.Setup(r => r.IsStaffAsync(gymId, staffUserId, default)).ReturnsAsync(true);
        _gymRepo.Setup(r => r.DeleteAsync(gymId, default)).Returns(Task.CompletedTask);

        var handler = new DeleteGymHandler(_gymRepo.Object, _staffRepo.Object);

        // Act
        var result = await handler.HandleAsync(new DeleteGymCommand(gymId), staffUserId, default);

        // Assert
        Assert.True(result.IsSuccess);
        _gymRepo.Verify(r => r.DeleteAsync(gymId, default), Times.Once);
    }
}



