using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace UnitTests.Features.Authorization.Groups;

public class GroupRepositoryTests
{
    private readonly AuthorizationDbContext _context;
    private readonly GroupRepository _repository;

    public GroupRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AuthorizationDbContext(options);
        _repository = new GroupRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ValidGroup_SavesSuccessfully()
    {
        // Arrange
        var group = new Group
        {
            Name = "Engineering Team",
            Description = "Team responsible for engineering",
            CreatedById = 1
        };

        // Act
        await _repository.AddAsync(group, CancellationToken.None);

        // Assert
        var savedGroup = await _context.Groups.FirstOrDefaultAsync();
        Assert.NotNull(savedGroup);
        Assert.Equal("Engineering Team", savedGroup.Name);
        Assert.Equal(1, savedGroup.CreatedById);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingGroup_ReturnsGroup()
    {
        // Arrange
        var group = new Group
        {
            Name = "Test Group",
            CreatedById = 1
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(group.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(group.Id, result.Id);
        Assert.Equal("Test Group", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentGroup_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_MultipleGroups_ReturnsAll()
    {
        // Arrange
        var groups = new List<Group>
        {
            new Group { Name = "Group 1", CreatedById = 1 },
            new Group { Name = "Group 2", CreatedById = 2 },
            new Group { Name = "Group 3", CreatedById = 1 }
        };

        _context.Groups.AddRange(groups);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_NoGroups_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task AddUserToGroupAsync_ValidInput_AddsUserSuccessfully()
    {
        // Arrange
        var group = new Group { Name = "Test Group", CreatedById = 1 };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        await _repository.AddUserToGroupAsync(1, group.Id, GroupRole.Member, CancellationToken.None);

        // Assert
        var userGroup = await _context.UserGroups
            .FirstOrDefaultAsync(ug => ug.UserId == 1 && ug.GroupId == group.Id);
        Assert.NotNull(userGroup);
        Assert.Equal(GroupRole.Member, userGroup.Role);
    }

    [Fact]
    public async Task UserBelongsToGroupAsync_UserInGroup_ReturnsTrue()
    {
        // Arrange
        var group = new Group { Name = "Test Group", CreatedById = 1 };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var userGroup = new UserGroup { UserId = 2, GroupId = group.Id, Role = GroupRole.Member };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UserBelongsToGroupAsync(2, group.Id, CancellationToken.None);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task UserBelongsToGroupAsync_UserNotInGroup_ReturnsFalse()
    {
        // Arrange
        var group = new Group { Name = "Test Group", CreatedById = 1 };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.UserBelongsToGroupAsync(2, group.Id, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetUserRoleInGroupAsync_UserInGroup_ReturnsRole()
    {
        // Arrange
        var group = new Group { Name = "Test Group", CreatedById = 1 };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var userGroup = new UserGroup { UserId = 2, GroupId = group.Id, Role = GroupRole.Owner };
        _context.UserGroups.Add(userGroup);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserRoleInGroupAsync(2, group.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(GroupRole.Owner, result);
    }

    [Fact]
    public async Task GetUserRoleInGroupAsync_UserNotInGroup_ReturnsNull()
    {
        // Arrange
        var group = new Group { Name = "Test Group", CreatedById = 1 };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserRoleInGroupAsync(2, group.Id, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByUserIdAsync_UserInMultipleGroups_ReturnsAllGroups()
    {
        // Arrange
        var groups = new List<Group>
        {
            new Group { Name = "Group 1", CreatedById = 1 },
            new Group { Name = "Group 2", CreatedById = 1 },
            new Group { Name = "Group 3", CreatedById = 2 }
        };

        _context.Groups.AddRange(groups);
        await _context.SaveChangesAsync();

        var userGroups = new List<UserGroup>
        {
            new UserGroup { UserId = 5, GroupId = groups[0].Id, Role = GroupRole.Owner },
            new UserGroup { UserId = 5, GroupId = groups[1].Id, Role = GroupRole.Member },
            new UserGroup { UserId = 6, GroupId = groups[2].Id, Role = GroupRole.Member }
        };

        _context.UserGroups.AddRange(userGroups);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(5, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByUserIdAsync_UserNotInAnyGroup_ReturnsEmpty()
    {
        // Arrange
        var group = new Group { Name = "Group 1", CreatedById = 1 };
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

}



