using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Domains.Training;

[Collection("SQL Server Write Operations")]
public class WorkoutPlanningScopeEndpointsTests(SqlServerFixture fixture) : IAsyncLifetime
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

    [Fact]
    public async Task CreateWorkoutPlan_WithoutScope_ReturnsForbidden()
    {
        var auth = await SeedUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var body = new
        {
            targetUserId = 1,
            name = "Push Day",
            notes = "notes",
            exercises = new[]
            {
                new
                {
                    exerciseId = 1,
                    sets = new[]
                    {
                        new { repetitions = 10, load = 20m, loadUnit = "kg", setType = "working", rpe = 8, restSeconds = 90 }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateWorkoutTemplate_WithoutScope_ReturnsForbidden()
    {
        var auth = await SeedUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var body = new
        {
            name = "Template A",
            notes = "notes",
            exercises = new[]
            {
                new
                {
                    exerciseId = 1,
                    sets = new[]
                    {
                        new { repetitions = 12, load = 15m, loadUnit = "kg", setType = "working", rpe = 7, restSeconds = 60 }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/training/workout-templates", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(int UserId, string Token)> SeedUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }
}


