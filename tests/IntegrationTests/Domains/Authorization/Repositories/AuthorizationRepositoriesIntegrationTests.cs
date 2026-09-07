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
}
