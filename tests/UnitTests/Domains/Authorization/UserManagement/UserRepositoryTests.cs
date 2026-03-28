using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace UnitTests.Domains.Authorization.UserManagement;

public class UserRepositoryTests
{
    private readonly AuthorizationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AuthorizationDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ValidUser_SavesSuccessfully()
    {
        // Arrange
        var user = new User
        {
            FirebaseUid = "firebase-123",
            Email = "test@example.com",
            DisplayName = "Test User",
            IsActive = true
        };

        // Act
        await _repository.AddAsync(user, CancellationToken.None);

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync();
        Assert.NotNull(savedUser);
        Assert.Equal("firebase-123", savedUser.FirebaseUid);
        Assert.Equal("test@example.com", savedUser.Email);
        Assert.Equal("Test User", savedUser.DisplayName);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            FirebaseUid = "firebase-456",
            Email = "user@example.com",
            DisplayName = "John Doe",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(user.Id, result.Id);
        Assert.Equal("user@example.com", result.Email);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByFirebaseUidAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            FirebaseUid = "firebase-789",
            Email = "firebase@example.com",
            DisplayName = "Firebase User",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByFirebaseUidAsync("firebase-789", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("firebase-789", result.FirebaseUid);
        Assert.Equal("firebase@example.com", result.Email);
    }

    [Fact]
    public async Task GetByFirebaseUidAsync_NonexistentUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByFirebaseUidAsync("nonexistent-uid", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByEmailAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = new User
        {
            FirebaseUid = "firebase-email",
            Email = "email@example.com",
            DisplayName = "Email User",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("email@example.com", CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("email@example.com", result.Email);
    }

    [Fact]
    public async Task GetByEmailAsync_NonexistentUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com", CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_MultipleUsers_ReturnsAll()
    {
        // Arrange
        var users = new List<User>
        {
            new User { FirebaseUid = "uid-1", Email = "user1@example.com", IsActive = true },
            new User { FirebaseUid = "uid-2", Email = "user2@example.com", IsActive = true },
            new User { FirebaseUid = "uid-3", Email = "user3@example.com", IsActive = false }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_NoUsers_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task UpdateAsync_ExistingUser_UpdatesSuccessfully()
    {
        // Arrange
        var user = new User
        {
            FirebaseUid = "firebase-update",
            Email = "update@example.com",
            DisplayName = "Original Name",
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var originalUpdatedAt = user.UpdatedAt;
        await Task.Delay(10); // Ensure timestamp difference

        // Act
        user.DisplayName = "Updated Name";
        await _repository.UpdateAsync(user, CancellationToken.None);

        // Assert
        var updated = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated Name", updated.DisplayName);
        Assert.True(updated.UpdatedAt > originalUpdatedAt);
    }
}


