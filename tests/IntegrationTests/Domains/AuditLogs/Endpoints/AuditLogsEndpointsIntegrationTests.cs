using System.Net;
using System.Net.Http.Headers;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Domains.AuditLogs.Endpoints;

[Collection("SQL Server Write Operations")]
public sealed class AuditLogsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    [InlineData(true, HttpStatusCode.OK)]
    [InlineData(false, HttpStatusCode.Forbidden)]
    public async Task GetAuditLogsEndpoint_ShouldRespectPlatformAdminCapability(bool asAdmin, HttpStatusCode expected)
    {
        var token = await SeedAuthorizedUserTokenAsync(asAdmin);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/audit-logs?pageSize=5");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("invalid-cursor")]
    [InlineData("$$$")]
    public async Task GetAuditLogsEndpoint_ShouldReturnBadRequestForInvalidCursor(string invalidCursor)
    {
        var token = await SeedAuthorizedUserTokenAsync(asAdmin: true);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync($"/api/audit-logs?cursor={invalidCursor}&pageSize=5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // native-authorization-model: GET /api/audit-logs requires PlatformRoleType.Admin
    // (capability:platform.audit_logs.read). asAdmin=false proves a non-admin still gets denied.
    private async Task<string> SeedAuthorizedUserTokenAsync(bool asAdmin)
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        if (asAdmin)
        {
            await using var gymContext = fixture.CreateGymManagementDbContext();
            await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);
        }

        return TestFirebaseService.CreateToken(user.FirebaseUid, user.Email);
    }
}
