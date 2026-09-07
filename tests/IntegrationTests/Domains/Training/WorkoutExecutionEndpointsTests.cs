using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Domains.Training;

[Collection("SQL Server Write Operations")]
public class WorkoutExecutionEndpointsTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    // ponytail: "Finish_WhenUserDoesNotHaveFinishScope_ReturnsForbidden" removed — WorkoutsController no
    // longer carries RequireScopesAttribute (native-authorization-model Phase 3). Hitting Finish for a
    // non-existent session now falls through to the handler and returns NotFound (not Forbidden), since
    // there's no scope gate left to short-circuit the request. Owner/non-owner authorization coverage for
    // Finish (and the other Workouts endpoints) lives in Endpoints/WorkoutsEndpointsIntegrationTests.cs.

    [Fact]
    public async Task Finish_ForNonExistentSession_ReturnsNotFound()
    {
        var auth = await SeedUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var body = new
        {
            endedAtUtc = DateTime.UtcNow,
            perceivedExertion = 7
        };

        var response = await _client.PostAsJsonAsync("/api/training/workouts/000000000000000000000000/finish", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteRoute_IsRemovedAndReturnsNotFound()
    {
        var auth = await SeedUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var body = new
        {
            endedAtUtc = DateTime.UtcNow,
            perceivedExertion = 7
        };

        var response = await _client.PostAsJsonAsync("/api/training/workouts/session-1/complete", body);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(int UserId, string Token)> SeedUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }
}


