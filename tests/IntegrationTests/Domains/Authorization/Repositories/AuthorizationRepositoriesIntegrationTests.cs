using IntegrationTests.Infrastructure;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace IntegrationTests.Domains.Authorization.Repositories;

[Collection("SQL Server Write Operations")]
public sealed class AuthorizationRepositoriesIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("u-1", "u1@test.com")]
    [InlineData("u-2", "u2@test.com")]
    public async Task UserRepository_AddAndGetByFirebaseUid_ShouldPersistAndReturn(string uid, string email)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var repository = new UserRepository(context);

        await repository.AddAsync(new User { FirebaseUid = uid, Email = email, IsActive = true }, CancellationToken.None);
        var found = await repository.GetByFirebaseUidAsync(uid, CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(email, found.Email);
    }

    [Theory]
    [InlineData(GroupRole.Owner)]
    [InlineData(GroupRole.Administrator)]
    [InlineData(GroupRole.Member)]
    public async Task GroupRepository_AddUserToGroup_ShouldPersistMembership(GroupRole role)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var groupRepository = new GroupRepository(context);

        var creator = await TestDataSeeder.SeedUserAsync(context, $"creator-{role}", CancellationToken.None);
        var member = await TestDataSeeder.SeedUserAsync(context, $"member-{role}", CancellationToken.None);
        var group = await TestDataSeeder.SeedGroupAsync(context, $"group-{role}", creator.Id, CancellationToken.None);

        await groupRepository.AddUserToGroupAsync(member.Id, group.Id, role, CancellationToken.None);

        var persistedRole = await groupRepository.GetUserRoleInGroupAsync(member.Id, group.Id, CancellationToken.None);
        Assert.Equal(role, persistedRole);
    }

    [Theory]
    [InlineData("finance", "reports", "read")]
    [InlineData("finance", "reports", "write")]
    public async Task ScopeRepository_AssignScopeToUser_ShouldReturnInUserScopes(string domain, string subdomain, string action)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var scopeRepository = new ScopeRepository(context);

        var user = await TestDataSeeder.SeedUserAsync(context, $"scope-{action}", CancellationToken.None);
        var scope = await TestDataSeeder.SeedScopeAsync(context, domain, subdomain, action, CancellationToken.None);

        await scopeRepository.AssignScopeToUserAsync(user.Id, scope.Id, CancellationToken.None);

        var scopes = await scopeRepository.GetUserScopesAsync(user.Id, CancellationToken.None);
        Assert.Contains(scopes, s => s.Id == scope.Id);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ScopeRepository_GetAllKeyset_ShouldHonorPageSize(int pageSize)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var scopeRepository = new ScopeRepository(context);

        await TestDataSeeder.SeedScopeAsync(context, $"demo{pageSize}", "a", "read", CancellationToken.None);
        await TestDataSeeder.SeedScopeAsync(context, $"demo{pageSize}", "a", "write", CancellationToken.None);
        await TestDataSeeder.SeedScopeAsync(context, $"demo{pageSize}", "a", "delete", CancellationToken.None);

        var page = await scopeRepository.GetAllKeysetAsync(lastScopeId: null, pageSize, CancellationToken.None);

        Assert.True(page.Count <= pageSize);
    }
}

