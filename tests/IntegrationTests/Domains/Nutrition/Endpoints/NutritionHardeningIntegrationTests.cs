namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Nutrition.Infrastructure.Mongo;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Relationships.Shared.Entities;

/// <summary>Concurrency and limits of the professional nutrition endpoints.</summary>
[Collection("SQL Server Write Operations")]
public sealed class NutritionHardeningIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

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
    public async Task AcceptInvite_ConcurrentAccepts_OnlyOneClientWinsTheSingleUseToken()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var clients = new[] { await SeedUserAsync(), await SeedUserAsync(), await SeedUserAsync(), await SeedUserAsync(), await SeedUserAsync() };
        As(pro);
        var token = (await (await _client.PostAsync("/api/nutrition/clients/invites", null)).Content.ReadFromJsonAsync<InvitePayload>())!.Token;

        var responses = await Task.WhenAll(clients.Select(async c =>
        {
            using var http = _factory.CreateClient();
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", c.Token);
            return await http.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token });
        }));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.OK), r => Assert.Equal(HttpStatusCode.Conflict, r.StatusCode));

        await using var relationships = fixture.CreateRelationshipsDbContext();
        Assert.Equal(1, await relationships.ProfessionalClientRelationships
            .CountAsync(r => r.ProfessionalUserId == pro.UserId && r.Status == RelationshipStatus.Active));
    }

    [Fact]
    public async Task Invites_CanBeListedAndRevoked_AndARevokedTokenCannotBeAccepted()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var other = await SeedUserAsync(nutritionist: true);
        var client = await SeedUserAsync();
        As(pro);
        var first = (await (await _client.PostAsync("/api/nutrition/clients/invites", null)).Content.ReadFromJsonAsync<InvitePayload>())!;
        var second = (await (await _client.PostAsync("/api/nutrition/clients/invites", null)).Content.ReadFromJsonAsync<InvitePayload>())!;
        Assert.True(first.InviteId > 0);

        var page = await _client.GetFromJsonAsync<Page<InviteItemPayload>>("/api/nutrition/clients/invites?pageSize=1");
        Assert.Single(page!.Items);
        Assert.NotNull(page.NextCursor);
        var rest = await _client.GetFromJsonAsync<Page<InviteItemPayload>>($"/api/nutrition/clients/invites?pageSize=10&cursor={Uri.EscapeDataString(page.NextCursor!)}");
        Assert.Equal([first.InviteId, second.InviteId], page.Items.Concat(rest!.Items).Select(i => i.InviteId).ToArray());

        As(other);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.DeleteAsync($"/api/nutrition/clients/invites/{first.InviteId}")).StatusCode);

        As(pro);
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/nutrition/clients/invites/{first.InviteId}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/nutrition/clients/invites/{first.InviteId}")).StatusCode);
        var afterRevoke = await _client.GetFromJsonAsync<Page<InviteItemPayload>>("/api/nutrition/clients/invites");
        Assert.Equal([second.InviteId], afterRevoke!.Items.Select(i => i.InviteId).ToArray());

        As(client);
        Assert.Equal(HttpStatusCode.Conflict,
            (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = first.Token })).StatusCode);
        Assert.Equal(HttpStatusCode.OK,
            (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = second.Token })).StatusCode);

        As(pro);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.DeleteAsync($"/api/nutrition/clients/invites/{second.InviteId}")).StatusCode);
    }

    [Fact]
    public async Task Invite_BeyondThePendingLimit_Returns409_UntilOneIsRevoked()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        As(pro);
        InvitePayload? last = null;
        for (var i = 0; i < 20; i++)
        {
            var response = await _client.PostAsync("/api/nutrition/clients/invites", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            last = await response.Content.ReadFromJsonAsync<InvitePayload>();
        }

        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsync("/api/nutrition/clients/invites", null)).StatusCode);

        await _client.DeleteAsync($"/api/nutrition/clients/invites/{last!.InviteId}");
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsync("/api/nutrition/clients/invites", null)).StatusCode);
    }

    [Fact]
    public async Task CreateMealPlan_WithClientId_IsIdempotent_AndOtherUsersIdIs409()
    {
        var owner = await SeedUserAsync();
        var other = await SeedUserAsync();
        As(owner);
        var food = await CreateFoodAsync("Idem Food");
        var id = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
        var body = new { id, name = "Idempotent", items = new[] { new { mealSlot = "lunch", foodId = food.Id, quantityGramsOrMl = 100m } } };

        var first = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", body);
        var second = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(id, (await second.Content.ReadFromJsonAsync<MealPlanPayload>())!.Id);
        var plans = await _client.GetFromJsonAsync<MealPlanPayload[]>("/api/nutrition/meal-plans");
        Assert.Single(plans!, p => p.Id == id);

        As(other);
        Assert.Equal(HttpStatusCode.Conflict, (await _client.PostAsJsonAsync("/api/nutrition/meal-plans", body)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new { id = "xyz", name = "x", items = body.items })).StatusCode);
    }

    [Fact]
    public async Task ActivateMealPlan_ReportsWhatWasApplied()
    {
        var user = await SeedUserAsync();
        As(user);
        var good = await CreateFoodAsync("Applied Food");
        var plan = await CreatePlanAsync(good.Id, good.Id);

        var response = await _client.PostAsync($"/api/nutrition/meal-plans/{plan.Id}/activate?date={Today:yyyy-MM-dd}", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = (await response.Content.ReadFromJsonAsync<ActivatePayload>())!;
        Assert.Equal(2, payload.AppliedItemCount);
        Assert.Equal(2, payload.TotalItemCount);
        Assert.Empty(payload.UnavailableItems);
        Assert.True(payload.Plan.IsActive);
    }

    [Fact]
    public async Task ActivateMealPlan_WhenTheActivationFails_LeavesNeitherDiaryEntriesNorAnActivePlan()
    {
        var user = await SeedUserAsync();
        As(user);
        var food = await CreateFoodAsync("Atomic Food");
        var plan = await CreatePlanAsync(food.Id, food.Id);

        using var failing = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IMealPlanRepository>();
            services.AddScoped<IMealPlanRepository>(sp =>
                new FailingActivationRepository(ActivatorUtilities.CreateInstance<MongoMealPlanRepository>(sp)));
        })).CreateClient();
        failing.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

        var date = Today.AddDays(-3);
        var response = await failing.PostAsync($"/api/nutrition/meal-plans/{plan.Id}/activate?date={date:yyyy-MM-dd}", null);

        Assert.False(response.IsSuccessStatusCode);
        var diary = await _client.GetFromJsonAsync<DiaryPayload>($"/api/nutrition/diary?date={date:yyyy-MM-dd}");
        Assert.Equal(0, diary!.Totals.Kcal);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/nutrition/meal-plans/active")).StatusCode);

        // The same call on the healthy host works, so the failure left nothing behind that blocks a retry.
        var retry = await _client.PostAsync($"/api/nutrition/meal-plans/{plan.Id}/activate?date={date:yyyy-MM-dd}", null);
        Assert.Equal(HttpStatusCode.OK, retry.StatusCode);
    }

    [Fact]
    public async Task ActivateMealPlan_SwitchingPlans_KeepsExactlyOneActive()
    {
        var user = await SeedUserAsync();
        As(user);
        var food = await CreateFoodAsync("Switch Food");
        var planA = await CreatePlanAsync(food.Id);
        var planB = await CreatePlanAsync(food.Id);

        await _client.PostAsync($"/api/nutrition/meal-plans/{planA.Id}/activate?date={Today:yyyy-MM-dd}", null);
        await _client.PostAsync($"/api/nutrition/meal-plans/{planB.Id}/activate?date={Today:yyyy-MM-dd}", null);

        var plans = await _client.GetFromJsonAsync<MealPlanPayload[]>("/api/nutrition/meal-plans");
        Assert.Equal([planB.Id], plans!.Where(p => p.IsActive).Select(p => p.Id).ToArray());
    }

    [Fact]
    public async Task Measurements_RejectFutureDatesAndLongRanges_AndPaginate()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var client = await SeedUserAsync();
        await LinkAsync(pro, client);
        As(pro);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/measurements", new { date = Today.AddDays(5), weightKg = 80 })).StatusCode);

        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.Created,
                (await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/measurements", new { date = Today.AddDays(-i), weightKg = 80 + i })).StatusCode);

        var first = await _client.GetFromJsonAsync<Page<MeasurementPayload>>($"/api/nutrition/users/{client.UserId}/measurements?pageSize=2");
        Assert.Equal(2, first!.Items.Length);
        Assert.NotNull(first.NextCursor);
        var second = await _client.GetFromJsonAsync<Page<MeasurementPayload>>($"/api/nutrition/users/{client.UserId}/measurements?pageSize=2&cursor={Uri.EscapeDataString(first.NextCursor!)}");
        Assert.Single(second!.Items);
        Assert.Null(second.NextCursor);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/measurements?from={Today.AddDays(-400):yyyy-MM-dd}&to={Today:yyyy-MM-dd}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/measurements?cursor=garbage")).StatusCode);
    }

    [Fact]
    public async Task Comments_PaginateAndLimitTheRange()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var client = await SeedUserAsync();
        await LinkAsync(pro, client);
        As(pro);
        for (var i = 0; i < 3; i++)
            await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/diary/comments", new { date = Today, text = $"c{i}" });

        var from = Today.AddDays(-5);
        var first = await _client.GetFromJsonAsync<Page<CommentPayload>>($"/api/nutrition/users/{client.UserId}/diary/comments?from={from:yyyy-MM-dd}&to={Today:yyyy-MM-dd}&pageSize=2");
        Assert.Equal(["c0", "c1"], first!.Items.Select(c => c.Text).ToArray());
        var second = await _client.GetFromJsonAsync<Page<CommentPayload>>($"/api/nutrition/users/{client.UserId}/diary/comments?from={from:yyyy-MM-dd}&to={Today:yyyy-MM-dd}&pageSize=2&cursor={Uri.EscapeDataString(first.NextCursor!)}");
        Assert.Equal(["c2"], second!.Items.Select(c => c.Text).ToArray());
        Assert.Null(second.NextCursor);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/diary/comments?from={Today.AddDays(-400):yyyy-MM-dd}&to={Today:yyyy-MM-dd}")).StatusCode);
    }

    [Fact]
    public async Task ClientsAdherence_PaginatesClients()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var clients = new[] { await SeedUserAsync(name: "A"), await SeedUserAsync(name: "B"), await SeedUserAsync(name: "C") };
        foreach (var c in clients)
            await LinkAsync(pro, c);
        As(pro);

        var first = await _client.GetFromJsonAsync<Page<AdherencePayload>>("/api/nutrition/clients/adherence?pageSize=2");
        Assert.Equal(2, first!.Items.Length);
        Assert.NotNull(first.NextCursor);
        var second = await _client.GetFromJsonAsync<Page<AdherencePayload>>($"/api/nutrition/clients/adherence?pageSize=2&cursor={Uri.EscapeDataString(first.NextCursor!)}");
        Assert.Single(second!.Items);
        Assert.Null(second.NextCursor);
        Assert.Equal(clients.Select(c => c.UserId).Order().ToArray(), first.Items.Concat(second.Items).Select(i => i.ClientUserId).Order().ToArray());
        Assert.Equal(["A", "B", "C"], first.Items.Concat(second.Items).Select(i => i.Name!).Order().ToArray());
    }

    [Fact]
    public async Task ProfessionalAccessToClientData_IsAudited_WithoutStoringTheRequestBody()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var client = await SeedUserAsync();
        await LinkAsync(pro, client);
        As(pro);

        await _client.GetAsync($"/api/nutrition/users/{client.UserId}/profile");
        await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/measurements", new { date = Today, weightKg = 77.7, notes = "segredo-clinico" });

        await using var audit = fixture.CreateAuditLogsDbContext();
        var entries = await audit.AuditLogEntries
            .Where(e => e.UserEmail == pro.Email && e.Endpoint.Contains($"/api/nutrition/users/{client.UserId}/"))
            .ToListAsync();

        var read = Assert.Single(entries, e => e.HttpMethod == "GET" && e.Endpoint.EndsWith("/profile"));
        Assert.Equal(200, read.StatusCode);
        var write = Assert.Single(entries, e => e.HttpMethod == "POST" && e.Endpoint.EndsWith("/measurements"));
        Assert.Null(write.RequestBodyJson);
        Assert.DoesNotContain(entries, e => (e.RequestBodyJson ?? "").Contains("segredo-clinico"));
    }

    private async Task<MealPlanPayload> CreatePlanAsync(params string[] foodIds)
    {
        var response = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new
        {
            name = $"Plan {Guid.NewGuid():N}"[..14],
            items = foodIds.Select(id => new { mealSlot = "lunch", foodId = id, quantityGramsOrMl = 100m }).ToArray()
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MealPlanPayload>())!;
    }

    private async Task<FoodPayload> CreateFoodAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"{name}-{Guid.NewGuid():N}",
            barcode = $"789{Guid.NewGuid():N}"[..13],
            macrosPer100 = new { kcal = 200, proteinG = 10, carbG = 30, fatG = 5 }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FoodPayload>())!;
    }

    private async Task LinkAsync(TestUser pro, TestUser client)
    {
        As(pro);
        var token = (await (await _client.PostAsync("/api/nutrition/clients/invites", null)).Content.ReadFromJsonAsync<InvitePayload>())!.Token;
        As(client);
        Assert.Equal(HttpStatusCode.OK, (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token })).StatusCode);
    }

    private void As(TestUser user) =>
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", user.Token);

    private async Task<TestUser> SeedUserAsync(bool nutritionist = false, string? name = null)
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        if (name is not null)
        {
            user.DisplayName = name;
            await context.SaveChangesAsync();
        }

        if (nutritionist)
        {
            await using var gym = fixture.CreateGymManagementDbContext();
            gym.UserPlatformRoles.Add(new UserPlatformRole { UserId = user.Id, Role = PlatformRoleType.Nutritionist, IsActive = true });
            await gym.SaveChangesAsync();
        }

        return new TestUser(user.Id, user.Email, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed class FailingActivationRepository(IMealPlanRepository inner) : IMealPlanRepository
    {
        public Task CreateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken) => inner.CreateAsync(mealPlan, cancellationToken);
        public Task<MealPlanDocument?> GetByIdAsync(string id, CancellationToken cancellationToken) => inner.GetByIdAsync(id, cancellationToken);
        public Task<IReadOnlyList<MealPlanDocument>> GetByUserAsync(int userId, CancellationToken cancellationToken) => inner.GetByUserAsync(userId, cancellationToken);
        public Task<MealPlanDocument?> GetActiveAsync(int userId, CancellationToken cancellationToken) => inner.GetActiveAsync(userId, cancellationToken);
        public Task UpdateAsync(MealPlanDocument mealPlan, CancellationToken cancellationToken) => inner.UpdateAsync(mealPlan, cancellationToken);
        public Task SetExclusiveActiveAsync(int userId, string? planId, DateTime nowUtc, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("simulated Mongo failure");
    }

    private sealed record Page<T>(T[] Items, string? NextCursor);
    private sealed record TestUser(int UserId, string Email, string Token);
    private sealed record InvitePayload(string Token, int InviteId);
    private sealed record InviteItemPayload(int InviteId);
    private sealed record FoodPayload(string Id);
    private sealed record MealPlanPayload(string Id, bool IsActive);
    private sealed record ActivatePayload(MealPlanPayload Plan, object[] UnavailableItems, int AppliedItemCount, int TotalItemCount);
    private sealed record TotalsPayload(int Kcal);
    private sealed record DiaryPayload(TotalsPayload Totals);
    private sealed record MeasurementPayload(int Id);
    private sealed record CommentPayload(int Id, string Text);
    private sealed record AdherencePayload(int ClientUserId, string? Name);
}
