using IntegrationTests.Infrastructure;
using ShapeUp.Features.Authorization.Infrastructure.Repositories;
using ShapeUp.Features.Authorization.UserManagement.GetUser;

namespace IntegrationTests.Domains.Authorization.Handlers;

[Collection("SQL Server Write Operations")]
public sealed class GetUserHandlerIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("get-uid-1")]
    [InlineData("get-uid-2")]
    public async Task GetUserHandler_ShouldReturnExistingUser(string firebaseUid)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var userRepository = new UserRepository(context);

        // Middleware is responsible for creation — seed the user to simulate that
        var seeded = await TestDataSeeder.SeedUserAsync(context, firebaseUid, CancellationToken.None);

        var handler = new GetUserHandler(userRepository);
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
        var handler = new GetUserHandler(new UserRepository(context));

        var result = await handler.HandleAsync(new GetUserQuery(unknownId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Error!.StatusCode);
    }
}
