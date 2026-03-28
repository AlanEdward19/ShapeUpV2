using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Shared.Data;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace UnitTests.Domains.Authorization;

public class ScopeRepositoryTests
{
    private readonly AuthorizationDbContext _context;
    private readonly ScopeRepository _repository;

    public ScopeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AuthorizationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new AuthorizationDbContext(options);
        _repository = new ScopeRepository(_context);
    }

    [Theory]
    [InlineData("users:profile:read", "users", "profile", "read")]
    [InlineData("groups:management:create", "groups", "management", "create")]
    [InlineData("orders:items:update", "orders", "items", "update")]
    public async Task AddAsync_ValidScope_SavesSuccessfully(
        string scopeName, string domain, string subdomain, string action)
    {
        // Arrange
        var scope = new Scope
        {
            Name = scopeName,
            Domain = domain,
            Subdomain = subdomain,
            Action = action,
            Description = "Test scope"
        };

        // Act
        await _repository.AddAsync(scope, CancellationToken.None);

        // Assert
        var saved = await _context.Scopes.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(scopeName, saved.Name);
        Assert.Equal(domain, saved.Domain);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task GetByIdAsync_ExistingScope_ReturnsScope(int scopeId)
    {
        // Arrange
        var scope = new Scope
        {
            Name = $"scope:{scopeId}",
            Domain = "domain",
            Subdomain = "sub",
            Action = "action"
        };

        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(scope.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal($"scope:{scopeId}", result.Name);
    }

    [Theory]
    [InlineData(999)]
    [InlineData(1000)]
    public async Task GetByIdAsync_NonexistentScope_ReturnsNull(int nonexistentId)
    {
        // Act
        var result = await _repository.GetByIdAsync(nonexistentId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("users:profile:read")]
    [InlineData("groups:management:create")]
    [InlineData("orders:items:delete")]
    public async Task GetByNameAsync_ExistingScope_ReturnsScope(string scopeName)
    {
        // Arrange
        var scope = new Scope
        {
            Name = scopeName,
            Domain = "domain",
            Subdomain = "sub",
            Action = "action"
        };

        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByNameAsync(scopeName, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(scopeName, result.Name);
    }

    [Theory]
    [InlineData("nonexistent:scope:name")]
    [InlineData("fake:permission:test")]
    public async Task GetByNameAsync_NonexistentScope_ReturnsNull(string scopeName)
    {
        // Act
        var result = await _repository.GetByNameAsync(scopeName, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Theory]
    [InlineData("users", "profile", "read")]
    [InlineData("groups", "management", "create")]
    [InlineData("orders", "items", "update")]
    public async Task GetByScopeFormatAsync_ExistingScope_ReturnsScope(
        string domain, string subdomain, string action)
    {
        // Arrange
        var scope = new Scope
        {
            Name = $"{domain}:{subdomain}:{action}",
            Domain = domain,
            Subdomain = subdomain,
            Action = action
        };

        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByScopeFormatAsync(domain, subdomain, action, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(domain, result.Domain);
        Assert.Equal(subdomain, result.Subdomain);
        Assert.Equal(action, result.Action);
    }

    [Theory]
    [InlineData("nonexistent", "sub", "action")]
    [InlineData("users", "fake", "read")]
    [InlineData("users", "profile", "fake")]
    public async Task GetByScopeFormatAsync_NonexistentScope_ReturnsNull(
        string domain, string subdomain, string action)
    {
        // Act
        var result = await _repository.GetByScopeFormatAsync(domain, subdomain, action, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllAsync_MultipleScopeNames_ReturnsAll()
    {
        // Arrange
        var scopes = new List<Scope>
        {
            new Scope { Name = "users:read", Domain = "users", Subdomain = "", Action = "read" },
            new Scope { Name = "users:write", Domain = "users", Subdomain = "", Action = "write" },
            new Scope { Name = "groups:create", Domain = "groups", Subdomain = "", Action = "create" },
            new Scope { Name = "orders:delete", Domain = "orders", Subdomain = "", Action = "delete" },
        };

        _context.Scopes.AddRange(scopes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public async Task GetAllAsync_NoScopes_ReturnsEmpty()
    {
        // Act
        var result = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task GetAllKeysetAsync_WithPageSize_RespectsPageSize(int pageSize)
    {
        // Arrange
        var scopes = new List<Scope>();
        for (int i = 1; i <= 5; i++)
        {
            scopes.Add(new Scope
            {
                Name = $"scope{i}",
                Domain = "domain",
                Subdomain = "sub",
                Action = "action"
            });
        }

        _context.Scopes.AddRange(scopes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllKeysetAsync(null, pageSize, CancellationToken.None);

        // Assert
        Assert.True(result.Count <= pageSize);
    }

    [Fact]
    public async Task AssignScopeToUserAsync_ValidInput_AssignsSuccessfully()
    {
        // Arrange
        var scope = new Scope
        {
            Name = "users:read",
            Domain = "users",
            Subdomain = "",
            Action = "read"
        };

        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();

        // Act
        await _repository.AssignScopeToUserAsync(1, scope.Id, CancellationToken.None);

        // Assert
        var userScope = await _context.UserScopes.FirstOrDefaultAsync(us => us.UserId == 1);
        Assert.NotNull(userScope);
        Assert.Equal(scope.Id, userScope.ScopeId);
    }

    [Fact]
    public async Task AssignScopeToGroupAsync_ValidInput_AssignsSuccessfully()
    {
        // Arrange
        var scope = new Scope
        {
            Name = "groups:manage",
            Domain = "groups",
            Subdomain = "",
            Action = "manage"
        };

        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();

        // Act
        await _repository.AssignScopeToGroupAsync(1, scope.Id, CancellationToken.None);

        // Assert
        var groupScope = await _context.GroupScopes.FirstOrDefaultAsync(gs => gs.GroupId == 1);
        Assert.NotNull(groupScope);
        Assert.Equal(scope.Id, groupScope.ScopeId);
    }

    [Fact]
    public async Task GetUserScopesAsync_DirectAndGroupScopes_ReturnsCombinedAndDeduplicated()
    {
        // Arrange
        var scope1 = new Scope { Name = "read", Domain = "users", Subdomain = "", Action = "read" };
        var scope2 = new Scope { Name = "write", Domain = "users", Subdomain = "", Action = "write" };
        var scope3 = new Scope { Name = "delete", Domain = "users", Subdomain = "", Action = "delete" };

        _context.Scopes.AddRange(scope1, scope2, scope3);

        var user = new User { FirebaseUid = "uid1", Email = "user@test.com", IsActive = true };
        _context.Users.Add(user);

        var group = new Group { Name = "group1", CreatedById = 1 };
        _context.Groups.Add(group);

        await _context.SaveChangesAsync();

        // Add direct scope
        _context.UserScopes.Add(new UserScope { UserId = user.Id, ScopeId = scope1.Id });

        // Add group scope
        _context.GroupScopes.Add(new GroupScope { GroupId = group.Id, ScopeId = scope2.Id });

        // Add user to group
        _context.UserGroups.Add(new UserGroup { UserId = user.Id, GroupId = group.Id, Role = GroupRole.Member });

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserScopesAsync(user.Id, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, s => s.Id == scope1.Id);
        Assert.Contains(result, s => s.Id == scope2.Id);
    }

    [Fact]
    public async Task GetGroupScopesAsync_MultipleScopesAssigned_ReturnsAll()
    {
        // Arrange
        var scopes = new List<Scope>
        {
            new Scope { Name = "create", Domain = "groups", Subdomain = "", Action = "create" },
            new Scope { Name = "delete", Domain = "groups", Subdomain = "", Action = "delete" },
            new Scope { Name = "manage", Domain = "groups", Subdomain = "", Action = "manage" },
        };

        _context.Scopes.AddRange(scopes);

        var group = new Group { Name = "group1", CreatedById = 1 };
        _context.Groups.Add(group);

        await _context.SaveChangesAsync();

        _context.GroupScopes.AddRange(
            new GroupScope { GroupId = group.Id, ScopeId = scopes[0].Id },
            new GroupScope { GroupId = group.Id, ScopeId = scopes[1].Id }
        );

        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetGroupScopesAsync(group.Id, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
    }

    [Theory]
    [InlineData("users")]
    [InlineData("groups")]
    [InlineData("orders")]
    public async Task GetByDomainAsync_ScopesWithDomain_ReturnsMatching(string domain)
    {
        // Arrange
        var scopes = new List<Scope>
        {
            new Scope { Name = $"{domain}:read", Domain = domain, Subdomain = "", Action = "read" },
            new Scope { Name = $"{domain}:write", Domain = domain, Subdomain = "", Action = "write" },
            new Scope { Name = "other:delete", Domain = "other", Subdomain = "", Action = "delete" },
        };

        _context.Scopes.AddRange(scopes);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByDomainAsync(domain, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(domain, s.Domain));
    }

    [Fact]
    public async Task UpdateAsync_ExistingScope_UpdatesSuccessfully()
    {
        // Arrange
        var scope = new Scope
        {
            Name = "original",
            Domain = "domain",
            Subdomain = "sub",
            Action = "action",
            Description = "Original description"
        };

        _context.Scopes.Add(scope);
        await _context.SaveChangesAsync();

        // Act
        scope.Description = "Updated description";
        await _repository.UpdateAsync(scope, CancellationToken.None);

        // Assert
        var updated = await _context.Scopes.FirstOrDefaultAsync(s => s.Id == scope.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated description", updated.Description);
    }
}

