namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.Infrastructure.Mongo;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;

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
    public async Task LegacyTrainingWeightRoute_ShouldReturnNotFound()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var legacyGet = await _client.GetAsync("/api/training/weight/registers?startDateUtc=2026-04-01T00:00:00Z&endDateUtc=2026-04-01T00:00:00Z");
        Assert.Equal(HttpStatusCode.NotFound, legacyGet.StatusCode);
    }

    [Fact]
    public async Task LegacyMongoWeightDocuments_ShouldBeReadableViaNutritionRepository()
    {
        var auth = await SeedAuthorizedUserAsync();

        using var scope = _factory.Services.CreateScope();
        var mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        var mongoOptions = scope.ServiceProvider.GetRequiredService<IOptions<NutritionMongoOptions>>().Value;
        var database = mongoClient.GetDatabase(mongoOptions.DatabaseName);

        var legacyUpdatedAt = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc);
        var targets = database.GetCollection<WeightTargetDocument>(mongoOptions.WeightTargetsCollectionName);
        var registers = database.GetCollection<WeightRegisterDocument>(mongoOptions.WeightRegistersCollectionName);

        await targets.InsertOneAsync(new WeightTargetDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = auth.UserId,
            TargetWeight = 78.5m,
            UpdatedAtUtc = legacyUpdatedAt
        });

        await registers.InsertOneAsync(new WeightRegisterDocument
        {
            Id = ObjectId.GenerateNewId().ToString(),
            UserId = auth.UserId,
            Day = "2025-11-15",
            Weight = 80.2m,
            CreatedAtUtc = legacyUpdatedAt,
            UpdatedAtUtc = legacyUpdatedAt
        });

        var repository = scope.ServiceProvider.GetRequiredService<IWeightTrackingRepository>();
        var target = await repository.GetTargetByUserIdAsync(auth.UserId, CancellationToken.None);
        var legacyRegisters = await repository.GetRegistersByRangeAsync(
            auth.UserId,
            new DateOnly(2025, 11, 1),
            new DateOnly(2025, 11, 30),
            CancellationToken.None);

        Assert.NotNull(target);
        Assert.Equal(78.5m, target!.TargetWeight);
        Assert.Single(legacyRegisters);
        Assert.Equal(80.2m, legacyRegisters[0].Weight);
        Assert.Equal("2025-11-15", legacyRegisters[0].Day);
    }

    [Fact]
    public async Task WeightTrackingEndpoints_ShouldUpsertTargetAndDailyRegisters_InSameDay()
    {
        // No scopes assigned: WeightTrackingController no longer requires RequireScopesAttribute
        // (native-authorization-model Phase 3, T22) — every endpoint here operates on the
        // authenticated caller's own data (HttpContext.GetUserId()), so authentication alone
        // is the gate. This proves the removal did not break self-access.
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var upsertTarget = await _client.PutAsJsonAsync("/api/nutrition/weight/target", new
        {
            targetWeight = 82.5m
        });

        Assert.Equal(HttpStatusCode.OK, upsertTarget.StatusCode);

        var date = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc);

        var firstRegister = await _client.PostAsJsonAsync("/api/nutrition/weight/registers", new
        {
            weight = 84.2m,
            dateUtc = date
        });
        Assert.Equal(HttpStatusCode.OK, firstRegister.StatusCode);

        var secondRegisterSameDay = await _client.PostAsJsonAsync("/api/nutrition/weight/registers", new
        {
            weight = 83.7m,
            dateUtc = date.AddHours(5)
        });
        Assert.Equal(HttpStatusCode.OK, secondRegisterSameDay.StatusCode);

        var get = await _client.GetAsync("/api/nutrition/weight/registers?startDateUtc=2026-04-01T00:00:00Z&endDateUtc=2026-04-01T00:00:00Z");
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

    private async Task<AuthorizedUser> SeedAuthorizedUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        return new AuthorizedUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record AuthorizedUser(int UserId, string Token);
    private sealed record GetWeightRegistersPayload(DateOnly StartDate, DateOnly EndDate, decimal? TargetWeight, WeightRegisterPayload[] Items);
    private sealed record WeightRegisterPayload(DateOnly Date, decimal Weight, DateTime UpdatedAtUtc);
}
