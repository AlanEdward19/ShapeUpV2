using IntegrationTests.Infrastructure;
using ShapeUp.Features.AuditLogs.GetAuditLogs;
using ShapeUp.Features.AuditLogs.Infrastructure.Repositories;

namespace IntegrationTests.Domains.AuditLogs.Handlers;

[Collection("SQL Server Write Operations")]
public sealed class GetAuditLogsHandlerIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task HandleAsync_ShouldReturnKeysetPage(int pageSize)
    {
        await using var context = fixture.CreateAuditLogsDbContext();
        var repository = new AuditLogRepository(context);

        await repository.AddAsync(TestDataSeeder.BuildAuditEntry("GET", "/api/groups", "a@test.com", 200), CancellationToken.None);
        await repository.AddAsync(TestDataSeeder.BuildAuditEntry("POST", "/api/scopes", "a@test.com", 201), CancellationToken.None);

        var handler = new GetAuditLogsHandler(repository);
        var result = await handler.HandleAsync(new GetAuditLogsQuery(null, pageSize, null, null, null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Items.Length <= pageSize);
    }

    [Theory]
    [InlineData("not-a-valid-cursor")]
    [InlineData("###")]
    public async Task HandleAsync_ShouldFailForInvalidCursor(string cursor)
    {
        await using var context = fixture.CreateAuditLogsDbContext();
        var handler = new GetAuditLogsHandler(new AuditLogRepository(context));

        var result = await handler.HandleAsync(new GetAuditLogsQuery(cursor, 5, null, null, null), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("validation_error", result.Error!.Code);
    }
}

