namespace IntegrationTests.Domains.Training.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class WeightTrackingEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task WeightTrackingEndpoints_ShouldUpsertTargetAndDailyRegisters_InSameDay()
    {
        var auth = await SeedAuthorizedUserAsync("training:workouts:update", "training:workouts:read");
        Authorize(auth.Token);

        var upsertTarget = await _client.PutAsJsonAsync("/api/training/weight/target", new
        {
            targetWeight = 82.5m
        });

        Assert.Equal(HttpStatusCode.OK, upsertTarget.StatusCode);

        var date = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);

        var firstRegister = await _client.PostAsJsonAsync("/api/training/weight/registers", new
        {
            weight = 84.2m,
            dateUtc = date
        });
        Assert.Equal(HttpStatusCode.OK, firstRegister.StatusCode);

        var secondRegisterSameDay = await _client.PostAsJsonAsync("/api/training/weight/registers", new
        {
            weight = 83.7m,
            dateUtc = date.AddHours(5)
        });
        Assert.Equal(HttpStatusCode.OK, secondRegisterSameDay.StatusCode);

        var get = await _client.GetAsync("/api/training/weight/registers?startDateUtc=2026-04-01T00:00:00Z&endDateUtc=2026-04-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);

        var payload = await get.Content.ReadFromJsonAsync<GetWeightRegistersPayload>();
        Assert.NotNull(payload);
        Assert.Equal(82.5m, payload!.TargetWeight);
        Assert.Single(payload.Items);
        Assert.Equal(83.7m, payload.Items[0].Weight);
    }

    private void Authorize(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<AuthorizedUser> SeedAuthorizedUserAsync(params string[] scopes)
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        await TestDataSeeder.AssignScopesToUserAsync(context, user.Id, scopes);

        return new AuthorizedUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record AuthorizedUser(int UserId, string Token);
    private sealed record GetWeightRegistersPayload(DateOnly StartDate, DateOnly EndDate, decimal? TargetWeight, WeightRegisterPayload[] Items);
    private sealed record WeightRegisterPayload(DateOnly Date, decimal Weight, DateTime UpdatedAtUtc);
}
