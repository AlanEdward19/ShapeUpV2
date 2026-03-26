using IntegrationTests.Infrastructure;
using ShapeUp.Features.AuditLogs.Infrastructure.Repositories;

namespace IntegrationTests.Domains.AuditLogs.Repositories;

[Collection("SQL Server Write Operations")]
public sealed class AuditLogRepositoryIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("GET", "/api/groups", "a@test.com")]
    [InlineData("POST", "/api/scopes", "b@test.com")]
    public async Task AddAsync_ShouldPersist(string method, string endpoint, string email)
    {
        await using var context = fixture.CreateAuditLogsDbContext();
        var repository = new AuditLogRepository(context);

        await repository.AddAsync(TestDataSeeder.BuildAuditEntry(method, endpoint, email, 200), CancellationToken.None);
        var page = await repository.GetPageAsync(null, 10, endpoint, method, email, CancellationToken.None);

        Assert.NotEmpty(page);
        Assert.Contains(page, e => e.Endpoint == endpoint && e.HttpMethod == method);
    }

    [Theory]
    [InlineData("/api/groups", "GET")]
    [InlineData("/api/scopes", "POST")]
    public async Task GetPageAsync_ShouldApplyFilters(string endpoint, string method)
    {
        await using var context = fixture.CreateAuditLogsDbContext();
        var repository = new AuditLogRepository(context);

        await repository.AddAsync(TestDataSeeder.BuildAuditEntry("GET", "/api/groups", "x@test.com", 200), CancellationToken.None);
        await repository.AddAsync(TestDataSeeder.BuildAuditEntry("POST", "/api/scopes", "y@test.com", 201), CancellationToken.None);

        var filtered = await repository.GetPageAsync(null, 10, endpoint, method, null, CancellationToken.None);

        Assert.NotEmpty(filtered);
        Assert.All(filtered, entry =>
        {
            Assert.Equal(endpoint, entry.Endpoint);
            Assert.Equal(method, entry.HttpMethod);
        });
    }
}

