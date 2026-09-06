namespace IntegrationTests.Domains.Relationships.Repositories;

using ShapeUp.Features.Relationships.Infrastructure.Repositories;
using ShapeUp.Features.Relationships.Shared.Entities;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class ProfessionalClientRelationshipRepositoryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    private static ProfessionalClientRelationship BuildRelationship(int professionalUserId, int clientUserId, string type) => new()
    {
        ProfessionalUserId = professionalUserId,
        ClientUserId = clientUserId,
        RelationshipType = type,
        Status = RelationshipStatus.Active,
        StartedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task CreateAsync_FirstActiveRelationship_Succeeds()
    {
        await using var ctx = fixture.CreateRelationshipsDbContext();
        var repo = new ProfessionalClientRelationshipRepository(ctx);
        var professionalUserId = Random.Shared.Next(100_000, 999_999);
        var clientUserId = Random.Shared.Next(100_000, 999_999);

        var result = await repo.CreateAsync(BuildRelationship(professionalUserId, clientUserId, "Training"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(RelationshipStatus.Active, result.Value.Status);
    }

    [Fact]
    public async Task CreateAsync_DuplicateActiveRelationshipSameTriple_ReturnsConflict()
    {
        var professionalUserId = Random.Shared.Next(100_000, 999_999);
        var clientUserId = Random.Shared.Next(100_000, 999_999);

        await using (var ctx = fixture.CreateRelationshipsDbContext())
        {
            var repo = new ProfessionalClientRelationshipRepository(ctx);
            var first = await repo.CreateAsync(BuildRelationship(professionalUserId, clientUserId, "Training"), CancellationToken.None);
            Assert.True(first.IsSuccess);
        }

        await using var ctx2 = fixture.CreateRelationshipsDbContext();
        var repo2 = new ProfessionalClientRelationshipRepository(ctx2);
        var second = await repo2.CreateAsync(BuildRelationship(professionalUserId, clientUserId, "Training"), CancellationToken.None);

        Assert.True(second.IsFailure);
        Assert.Equal("conflict", second.Error!.Code);
        Assert.Equal(409, second.Error.StatusCode);
    }

    [Fact]
    public async Task GetActiveAsync_AfterCreate_ReturnsPersistedRelationship()
    {
        var professionalUserId = Random.Shared.Next(100_000, 999_999);
        var clientUserId = Random.Shared.Next(100_000, 999_999);

        await using (var ctx = fixture.CreateRelationshipsDbContext())
        {
            var repo = new ProfessionalClientRelationshipRepository(ctx);
            await repo.CreateAsync(BuildRelationship(professionalUserId, clientUserId, "Nutrition"), CancellationToken.None);
        }

        await using var readCtx = fixture.CreateRelationshipsDbContext();
        var readRepo = new ProfessionalClientRelationshipRepository(readCtx);
        var found = await readRepo.GetActiveAsync(professionalUserId, clientUserId, "Nutrition", CancellationToken.None);

        Assert.NotNull(found);
        Assert.Equal(professionalUserId, found.ProfessionalUserId);
        Assert.Equal(clientUserId, found.ClientUserId);
    }
}
