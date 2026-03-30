using IntegrationTests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using ShapeUp.Features.Authorization.Groups.AddUserToGroup;
using ShapeUp.Features.Authorization.Groups.AssignScopeToGroup;
using ShapeUp.Features.Authorization.Groups.CreateGroup;
using ShapeUp.Features.Authorization.Groups.DeleteGroup;
using ShapeUp.Features.Authorization.Groups.GetGroups;
using ShapeUp.Features.Authorization.Groups.RemoveUserFromGroup;
using ShapeUp.Features.Authorization.Groups.UpdateUserRole;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.Scopes.AssignScopeToUser;
using ShapeUp.Features.Authorization.Scopes.CreateScope;
using ShapeUp.Features.Authorization.Scopes.GetScopes;
using ShapeUp.Features.Authorization.Scopes.GetUserScopes;
using ShapeUp.Features.Authorization.Scopes.RemoveScopeFromUser;
using ShapeUp.Features.Authorization.Scopes.Shared;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Authorization.Scopes.SyncUserScopes;
using ShapeUp.Features.Authorization.Scopes.SyncCurrentUserScopes;
using ShapeUp.Features.Authorization.UserManagement.GetUser;

namespace IntegrationTests.Domains.Authorization.Handlers;

[Collection("SQL Server Write Operations")]
public sealed class AuthorizationHandlersIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("Backend Team")]
    [InlineData("Platform Team")]
    public async Task CreateGroupHandler_ShouldCreateGroupAndOwnerMembership(string name)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var user = await TestDataSeeder.SeedUserAsync(context, name.Replace(" ", "-").ToLowerInvariant(), CancellationToken.None);
        var handler = new CreateGroupHandler(new GroupRepository(context));

        var result = await handler.HandleAsync(new CreateGroupCommand(name, "desc"), user.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(name, result.Value.Name);
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Administrator")]
    public async Task AddUserToGroupHandler_ShouldAddMemberWhenCurrentUserCanManage(string role)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var userRepository = new UserRepository(context);
        var groupRepository = new GroupRepository(context);

        var caseSuffix = role.ToLowerInvariant();
        var owner = await TestDataSeeder.SeedUserAsync(context, $"owner-add-{caseSuffix}", CancellationToken.None);
        var targetUser = await TestDataSeeder.SeedUserAsync(context, $"target-add-{caseSuffix}", CancellationToken.None);
        var group = await TestDataSeeder.SeedGroupAsync(context, $"group-add-{caseSuffix}", owner.Id, CancellationToken.None);
        await groupRepository.AddUserToGroupAsync(owner.Id, group.Id, GroupRole.Owner, CancellationToken.None);

        var handler = new AddUserToGroupHandler(groupRepository, userRepository);

        var result = await handler.HandleAsync(
            new AddUserToGroupCommand(targetUser.Id, group.Id, role),
            owner.Id,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("reports", "read")]
    [InlineData("reports", "write")]
    public async Task AssignScopeToGroupHandler_ShouldAssignWhenUserIsOwner(string subdomain, string action)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var groupRepository = new GroupRepository(context);
        var scopeRepository = new ScopeRepository(context);

        var caseSuffix = $"{subdomain}-{action}";
        var owner = await TestDataSeeder.SeedUserAsync(context, $"owner-scope-group-{caseSuffix}", CancellationToken.None);
        var group = await TestDataSeeder.SeedGroupAsync(context, $"group-scope-{caseSuffix}", owner.Id, CancellationToken.None);
        var scope = await TestDataSeeder.SeedScopeAsync(context, "groups", subdomain, action, CancellationToken.None);
        await groupRepository.AddUserToGroupAsync(owner.Id, group.Id, GroupRole.Owner, CancellationToken.None);

        var handler = new AssignScopeToGroupHandler(groupRepository, scopeRepository);
        var result = await handler.HandleAsync(group.Id, new AssignScopeToGroupCommand(scope.Id), owner.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Theory]
    [InlineData("group-delete-a")]
    [InlineData("group-delete-b")]
    public async Task DeleteGroupHandler_ShouldDeleteGroupForOwner(string suffix)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var groupRepository = new GroupRepository(context);

        var owner = await TestDataSeeder.SeedUserAsync(context, $"owner-{suffix}", CancellationToken.None);
        var group = await TestDataSeeder.SeedGroupAsync(context, $"{suffix}", owner.Id, CancellationToken.None);
        await groupRepository.AddUserToGroupAsync(owner.Id, group.Id, GroupRole.Owner, CancellationToken.None);

        var handler = new DeleteGroupHandler(groupRepository);
        var result = await handler.HandleAsync(new DeleteGroupCommand(group.Id), owner.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await groupRepository.GetByIdAsync(group.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetGroupsHandler_ShouldReturnKeysetPage(int pageSize)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var groupRepository = new GroupRepository(context);

        var owner = await TestDataSeeder.SeedUserAsync(context, $"owner-page-{pageSize}", CancellationToken.None);
        var g1 = await TestDataSeeder.SeedGroupAsync(context, $"group-{pageSize}-1", owner.Id, CancellationToken.None);
        var g2 = await TestDataSeeder.SeedGroupAsync(context, $"group-{pageSize}-2", owner.Id, CancellationToken.None);
        await groupRepository.AddUserToGroupAsync(owner.Id, g1.Id, GroupRole.Owner, CancellationToken.None);
        await groupRepository.AddUserToGroupAsync(owner.Id, g2.Id, GroupRole.Member, CancellationToken.None);

        var handler = new GetGroupsHandler(groupRepository);
        var result = await handler.HandleAsync(owner.Id, null, pageSize, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Items.Length <= pageSize);
    }

    [Theory]
    [InlineData(GroupRole.Owner)]
    [InlineData(GroupRole.Administrator)]
    public async Task RemoveUserFromGroupHandler_ShouldRemoveMember(GroupRole currentUserRole)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var groupRepository = new GroupRepository(context);

        var manager = await TestDataSeeder.SeedUserAsync(context, $"manager-{currentUserRole}", CancellationToken.None);
        var target = await TestDataSeeder.SeedUserAsync(context, $"target-{currentUserRole}", CancellationToken.None);
        var group = await TestDataSeeder.SeedGroupAsync(context, $"remove-{currentUserRole}", manager.Id, CancellationToken.None);

        await groupRepository.AddUserToGroupAsync(manager.Id, group.Id, currentUserRole, CancellationToken.None);
        await groupRepository.AddUserToGroupAsync(target.Id, group.Id, GroupRole.Member, CancellationToken.None);

        var handler = new RemoveUserFromGroupHandler(groupRepository);
        var result = await handler.HandleAsync(new RemoveUserFromGroupCommand(target.Id, group.Id), manager.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(await groupRepository.UserBelongsToGroupAsync(target.Id, group.Id, CancellationToken.None));
    }

    [Theory]
    [InlineData("Member")]
    [InlineData("Administrator")]
    public async Task UpdateUserRoleHandler_ShouldUpdateRole(string role)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var groupRepository = new GroupRepository(context);

        var owner = await TestDataSeeder.SeedUserAsync(context, $"owner-role-{role}", CancellationToken.None);
        var target = await TestDataSeeder.SeedUserAsync(context, $"target-role-{role}", CancellationToken.None);
        var group = await TestDataSeeder.SeedGroupAsync(context, $"group-role-{role}", owner.Id, CancellationToken.None);

        await groupRepository.AddUserToGroupAsync(owner.Id, group.Id, GroupRole.Owner, CancellationToken.None);
        await groupRepository.AddUserToGroupAsync(target.Id, group.Id, GroupRole.Member, CancellationToken.None);

        var handler = new UpdateUserRoleHandler(groupRepository);
        var result = await handler.HandleAsync(group.Id, new UpdateUserRoleCommand(target.Id, role), owner.Id, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(role, result.Value!.Role);
    }

    [Theory]
    [InlineData("billing", "invoice", "read")]
    [InlineData("billing", "invoice", "write")]
    public async Task CreateScopeHandler_ShouldCreateScope(string domain, string subdomain, string action)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var handler = new CreateScopeHandler(new ScopeRepository(context));

        var result = await handler.HandleAsync(new CreateScopeCommand(domain, subdomain, action, "desc"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal($"{domain}:{subdomain}:{action}", result.Value!.Name);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task GetScopesHandler_ShouldReturnKeysetPage(int pageSize)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var scopeRepository = new ScopeRepository(context);
        await TestDataSeeder.SeedScopeAsync(context, $"products{pageSize}", "catalog", "read", CancellationToken.None);
        await TestDataSeeder.SeedScopeAsync(context, $"products{pageSize}", "catalog", "write", CancellationToken.None);

        var handler = new GetScopesHandler(scopeRepository);
        var result = await handler.HandleAsync(cursor: null, pageSize, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Items.Length <= pageSize);
    }

    [Theory]
    [InlineData("alerts", "read")]
    [InlineData("alerts", "ack")]
    public async Task AssignAndRemoveScopeToUserHandlers_ShouldSyncFirebaseAndPersist(string subdomain, string action)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var scopeRepository = new ScopeRepository(context);
        var userRepository = new UserRepository(context);
        var firebase = new TestFirebaseService();
        var syncService = new UserScopeClaimsSyncService(userRepository, scopeRepository, firebase, NullLogger<UserScopeClaimsSyncService>.Instance);

        var user = await TestDataSeeder.SeedUserAsync(context, $"scope-user-{action}", CancellationToken.None);
        var scope = await TestDataSeeder.SeedScopeAsync(context, "ops", subdomain, action, CancellationToken.None);

        var assignHandler = new AssignScopeToUserHandler(
            scopeRepository,
            syncService,
            userRepository,
            new AssignScopeToUserValidator());
        var assignResult = await assignHandler.HandleAsync(user.Id, new AssignScopeToUserCommand(scope.Id), CancellationToken.None);
        Assert.True(assignResult.IsSuccess);

        var removeHandler = new RemoveScopeFromUserHandler(
            scopeRepository,
            syncService,
            userRepository,
            new RemoveScopeFromUserValidator());
        var removeResult = await removeHandler.HandleAsync(user.Id, new RemoveScopeFromUserCommand(scope.Id), CancellationToken.None);
        Assert.True(removeResult.IsSuccess);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1)]
    public async Task GetUserScopesHandler_ShouldReturnScopesWithKeyset(int? pageSize)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var scopeRepository = new ScopeRepository(context);

        var user = await TestDataSeeder.SeedUserAsync(context, $"user-scope-page-{pageSize}", CancellationToken.None);
        var scopeA = await TestDataSeeder.SeedScopeAsync(context, "crm", "lead", "read", CancellationToken.None);
        var scopeB = await TestDataSeeder.SeedScopeAsync(context, "crm", "lead", "write", CancellationToken.None);
        await scopeRepository.AssignScopeToUserAsync(user.Id, scopeA.Id, CancellationToken.None);
        await scopeRepository.AssignScopeToUserAsync(user.Id, scopeB.Id, CancellationToken.None);

        var handler = new GetUserScopesHandler(scopeRepository);
        var result = await handler.HandleAsync(user.Id, cursor: null, pageSize, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.Items);
    }

    [Theory]
    [InlineData("get-uid-1")]
    [InlineData("get-uid-2")]
    public async Task GetUserHandler_ShouldReturnExistingUser(string firebaseUid)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var userRepository = new UserRepository(context);
        var scopeRepository = new ScopeRepository(context);

        // Middleware is responsible for creation — seed the user to simulate that
        var seeded = await TestDataSeeder.SeedUserAsync(context, firebaseUid, CancellationToken.None);

        var handler = new GetUserHandler(userRepository, scopeRepository);
        var result = await handler.HandleAsync(new GetUserQuery(seeded.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(seeded.Id, result.Value!.UserId);
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(88888)]
    public async Task GetUserHandler_ShouldReturnFailureForUnknownId(int unknownId)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var handler = new GetUserHandler(new UserRepository(context), new ScopeRepository(context));

        var result = await handler.HandleAsync(new GetUserQuery(unknownId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Theory]
    [InlineData("sync", "read")]
    [InlineData("sync", "write")]
    public async Task SyncUserScopesHandler_ShouldSyncToFirebase(string subdomain, string action)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var scopeRepository = new ScopeRepository(context);
        var userRepository = new UserRepository(context);
        var firebase = new TestFirebaseService();
        var syncService = new UserScopeClaimsSyncService(userRepository, scopeRepository, firebase, NullLogger<UserScopeClaimsSyncService>.Instance);

        var user = await TestDataSeeder.SeedUserAsync(context, $"sync-user-{action}", CancellationToken.None);
        var scope = await TestDataSeeder.SeedScopeAsync(context, "ops", subdomain, action, CancellationToken.None);
        await scopeRepository.AssignScopeToUserAsync(user.Id, scope.Id, CancellationToken.None);

        var handler = new SyncUserScopesHandler(syncService, new SyncUserScopesValidator());
        var result = await handler.HandleAsync(new SyncUserScopesCommand(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.UserId);
        Assert.Equal(1, result.Value.ScopeCount);
    }

    [Theory]
    [InlineData("syncme", "read")]
    [InlineData("syncme", "write")]
    public async Task SyncCurrentUserScopesHandler_ShouldSyncToFirebase(string subdomain, string action)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var scopeRepository = new ScopeRepository(context);
        var userRepository = new UserRepository(context);
        var firebase = new TestFirebaseService();
        var syncService = new UserScopeClaimsSyncService(userRepository, scopeRepository, firebase, NullLogger<UserScopeClaimsSyncService>.Instance);

        var user = await TestDataSeeder.SeedUserAsync(context, $"syncme-user-{action}", CancellationToken.None);
        var scope = await TestDataSeeder.SeedScopeAsync(context, "portal", subdomain, action, CancellationToken.None);
        await scopeRepository.AssignScopeToUserAsync(user.Id, scope.Id, CancellationToken.None);

        var handler = new SyncCurrentUserScopesHandler(syncService, new SyncCurrentUserScopesValidator());
        var result = await handler.HandleAsync(new SyncCurrentUserScopesCommand(user.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(user.Id, result.Value!.UserId);
        Assert.Equal(1, result.Value.ScopeCount);
    }
}


