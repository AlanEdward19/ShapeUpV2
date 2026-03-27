using ShapeUp.Features.Authorization.Groups.CreateGroup;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace UnitTests.Domains.Authorization.Groups;

public class CreateGroupHandlerTests
{
    private readonly Mock<IGroupRepository> _mockGroupRepository;
    private readonly CreateGroupHandler _handler;

    public CreateGroupHandlerTests()
    {
        _mockGroupRepository = new Mock<IGroupRepository>();
        _handler = new CreateGroupHandler(_mockGroupRepository.Object);
    }

    public static IEnumerable<object[]> ValidGroupCases =>
        new List<object[]>
        {
            new object[] { "Engineering Team", "Team responsible for engineering", 1 },
            new object[] { "QA Team", "Quality Assurance Team", 2 },
            new object[] { "DevOps Team", "Infrastructure team", 3 },
            new object[] { "Product Team", "Product Management", 4 },
        };

    [Theory]
    [MemberData(nameof(ValidGroupCases))]
    public async Task HandleAsync_ValidCommand_CreatesGroupAndReturnsSuccess(
        string groupName, string? description, int createdById)
    {
        // Arrange
        var command = new CreateGroupCommand(groupName, description);
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.AddAsync(It.IsAny<Group>(), cancellationToken))
            .Callback<Group, CancellationToken>((group, _) => group.Id = createdById)
            .Returns(Task.CompletedTask);

        _mockGroupRepository
            .Setup(x => x.AddUserToGroupAsync(createdById, createdById, GroupRole.Owner, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, createdById, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(groupName, result.Value.Name);
        Assert.Equal(description, result.Value.Description);

        _mockGroupRepository.Verify(
            x => x.AddAsync(It.Is<Group>(g => g.Name == command.Name && g.CreatedById == createdById), cancellationToken),
            Times.Once);
        
        _mockGroupRepository.Verify(
            x => x.AddUserToGroupAsync(createdById, createdById, GroupRole.Owner, cancellationToken),
            Times.Once);
    }

    public static IEnumerable<object[]> GroupWithoutDescriptionCases =>
        new List<object[]>
        {
            new object[] { "QA Team", 2 },
            new object[] { "Frontend Team", 5 },
            new object[] { "Backend Team", 6 },
        };

    [Theory]
    [MemberData(nameof(GroupWithoutDescriptionCases))]
    public async Task HandleAsync_GroupWithoutDescription_CreatesSuccessfully(
        string groupName, int createdById)
    {
        // Arrange
        var command = new CreateGroupCommand(groupName, null);
        var cancellationToken = CancellationToken.None;

        _mockGroupRepository
            .Setup(x => x.AddAsync(It.IsAny<Group>(), cancellationToken))
            .Callback<Group, CancellationToken>((group, _) => group.Id = createdById)
            .Returns(Task.CompletedTask);

        _mockGroupRepository
            .Setup(x => x.AddUserToGroupAsync(createdById, createdById, GroupRole.Owner, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, createdById, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(groupName, result.Value.Name);
        Assert.Null(result.Value.Description);

        _mockGroupRepository.Verify(x => x.AddAsync(It.IsAny<Group>(), cancellationToken), Times.Once);
        _mockGroupRepository.Verify(
            x => x.AddUserToGroupAsync(createdById, createdById, GroupRole.Owner, cancellationToken),
            Times.Once);
    }

    public static IEnumerable<object[]> CreatorAssignedAsOwnerCases =>
        new List<object[]>
        {
            new object[] { "Team A", "Description A", 1 },
            new object[] { "Team B", "Description B", 5 },
            new object[] { "Team C", "Description C", 10 },
        };

    [Theory]
    [MemberData(nameof(CreatorAssignedAsOwnerCases))]
    public async Task HandleAsync_CreatorAssignedAsOwner_Correctly(
        string groupName, string description, int createdById)
    {
        // Arrange
        var command = new CreateGroupCommand(groupName, description);
        var cancellationToken = CancellationToken.None;

        Group? capturedGroup = null;
        _mockGroupRepository
            .Setup(x => x.AddAsync(It.IsAny<Group>(), cancellationToken))
            .Callback<Group, CancellationToken>((group, _) =>
            {
                capturedGroup = group;
                group.Id = createdById;
            })
            .Returns(Task.CompletedTask);

        _mockGroupRepository
            .Setup(x => x.AddUserToGroupAsync(createdById, createdById, GroupRole.Owner, cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.HandleAsync(command, createdById, cancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedGroup);
        Assert.Equal(createdById, capturedGroup.CreatedById);

        _mockGroupRepository.Verify(
            x => x.AddUserToGroupAsync(createdById, createdById, GroupRole.Owner, cancellationToken),
            Times.Once);
    }
}



