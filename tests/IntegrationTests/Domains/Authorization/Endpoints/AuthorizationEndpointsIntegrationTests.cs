using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Entities;

namespace IntegrationTests.Domains.Authorization.Endpoints;

[Collection("SQL Server Write Operations")]
public sealed class AuthorizationEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
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
    [InlineData("groups:management:create", HttpStatusCode.Created)]
    [InlineData("groups:management:delete", HttpStatusCode.Forbidden)]
    public async Task GroupCreateEndpoint_ShouldRespectScope(string grantedScope, HttpStatusCode expectedStatus)
    {
        var token = await SeedAuthorizedUserTokenAsync(grantedScope);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/groups", new { name = "api-group", description = "desc" });

        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Theory]
    [InlineData("scopes:management:create", "sales", "leads", "read", HttpStatusCode.Created)]
    [InlineData("groups:management:create", "sales", "leads", "write", HttpStatusCode.Forbidden)]
    public async Task ScopeCreateEndpoint_ShouldRespectScope(string grantedScope, string domain, string subdomain, string action, HttpStatusCode expected)
    {
        var token = await SeedAuthorizedUserTokenAsync(grantedScope);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/scopes", new { domain, subdomain, action, description = "d" });

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("user-a", "ua@test.com")]
    [InlineData("user-b", "ub@test.com")]
    public async Task UserGetEndpoint_ShouldReturnOk(string uid, string email)
    {
        var token = await SeedAuthorizedUserTokenAsync("groups:management:create");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await using var context = fixture.CreateAuthorizationDbContext();
        var user = new User
        {
            FirebaseUid = uid,
            Email = email,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var response = await _client.GetAsync($"/api/users/{user.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("audit:logs:read", HttpStatusCode.OK)]
    [InlineData("groups:management:create", HttpStatusCode.Forbidden)]
    public async Task AuditLogsEndpoint_ShouldRespectScopeFromAuthorizationDomain(string grantedScope, HttpStatusCode expected)
    {
        var token = await SeedAuthorizedUserTokenAsync(grantedScope);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/audit-logs?pageSize=2");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("", HttpStatusCode.BadRequest)]
    [InlineData("   ", HttpStatusCode.BadRequest)]
    public async Task GroupCreateEndpoint_ShouldValidatePayload(string invalidName, HttpStatusCode expected)
    {
        var token = await SeedAuthorizedUserTokenAsync("groups:management:create");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/groups", new { name = invalidName, description = "desc" });

        Assert.Equal(expected, response.StatusCode);
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

