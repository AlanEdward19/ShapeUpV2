using System.Net;
using System.Net.Http.Headers;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace IntegrationTests.Domains.AuditLogs.Endpoints;

[Collection("Integration SQL Server")]
public sealed class AuditLogsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync(CancellationToken.None);
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Theory]
    [InlineData("audit:logs:read", HttpStatusCode.OK)]
    [InlineData("groups:management:create", HttpStatusCode.Forbidden)]
    public async Task GetAuditLogsEndpoint_ShouldRespectScope(string grantedScope, HttpStatusCode expected)
    {
        var token = await SeedAuthorizedUserTokenAsync(grantedScope);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/audit-logs?pageSize=5");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid-cursor")]
    [InlineData("$$$")]
    public async Task GetAuditLogsEndpoint_ShouldReturnBadRequestForInvalidCursor(string invalidCursor)
    {
        var token = await SeedAuthorizedUserTokenAsync("audit:logs:read");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/audit-logs?cursor={invalidCursor}&pageSize=5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> SeedAuthorizedUserTokenAsync(params string[] scopes)
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        var scopeEntities = await context.Scopes.Where(s => scopes.Contains(s.Name)).ToListAsync();
        foreach (var scope in scopeEntities)
        {
            context.UserScopes.Add(new UserScope { UserId = user.Id, ScopeId = scope.Id });
        }

        await context.SaveChangesAsync();
        return TestFirebaseService.CreateToken(user.FirebaseUid, user.Email);
    }
}

