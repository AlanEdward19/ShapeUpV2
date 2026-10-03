namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using ShapeUp.Features.GymManagement.Shared.Entities;

/// <summary>Professional nutrition: invite/link, access control (403) and the data a nutritionist reads and prescribes.</summary>
[Collection("SQL Server Write Operations")]
public sealed class NutritionProfessionalEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task InviteAccept_LinksClient_ListsOnBothSides_AndEndingRevokesAccess()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var client = await SeedUserAsync(name: "Maria Aluna");
        await LinkAsync(pro, client);

        As(pro);
        var clients = await _client.GetFromJsonAsync<ClientPayload[]>("/api/nutrition/clients");
        Assert.Contains(clients!, c => c.ClientUserId == client.UserId && c.Name == "Maria Aluna");

        As(client);
        var nutritionists = await _client.GetFromJsonAsync<NutritionistPayload[]>("/api/nutrition/nutritionists");
        Assert.Contains(nutritionists!, n => n.NutritionistUserId == pro.UserId);

        As(pro);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/profile")).StatusCode);

        var end = await _client.DeleteAsync($"/api/nutrition/clients/{client.UserId}");
        Assert.Equal(HttpStatusCode.OK, end.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/profile")).StatusCode);

        As(client);
        Assert.Empty((await _client.GetFromJsonAsync<NutritionistPayload[]>("/api/nutrition/nutritionists"))!);
    }

    [Fact]
    public async Task Invite_WithoutNutritionCapability_Returns403_AndAcceptWithBadTokenReturns404()
    {
        var plain = await SeedUserAsync();
        As(plain);

        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsync("/api/nutrition/clients/invites", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/nutrition/clients")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync("/api/nutrition/clients/adherence")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token = "nope" })).StatusCode);
    }

    [Fact]
    public async Task UserRoutes_WithoutLink_Return403_ForNutritionistStrangerAndPlainUser_ButClientReadsOwn()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var stranger = await SeedUserAsync();
        var client = await SeedUserAsync();

        var date = Today.ToString("yyyy-MM-dd");
        var reads = new[]
        {
            $"/api/nutrition/users/{client.UserId}/diary?date={date}",
            $"/api/nutrition/users/{client.UserId}/profile",
            $"/api/nutrition/users/{client.UserId}/meal-plans",
            $"/api/nutrition/users/{client.UserId}/meal-plans/active",
            $"/api/nutrition/users/{client.UserId}/measurements",
            $"/api/nutrition/users/{client.UserId}/diary/comments?date={date}",
            $"/api/nutrition/users/{client.UserId}/fasting",
            $"/api/nutrition/users/{client.UserId}/fasting/history",
            $"/api/nutrition/users/{client.UserId}/hydration?date={date}"
        };

        foreach (var actor in new[] { pro, stranger })
        {
            As(actor);
            foreach (var url in reads)
                Assert.True(HttpStatusCode.Forbidden == (await _client.GetAsync(url)).StatusCode, $"{url} as {actor.UserId}");

            Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync($"/api/nutrition/users/{client.UserId}/restrictions", new { restrictions = "x" })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.PutAsJsonAsync($"/api/nutrition/users/{client.UserId}/goal",
                new { goal = new { kcal = 2000, proteinG = 150, carbG = 200, fatG = 60 } })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/measurements", new { date = Today, weightKg = 80 })).StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/diary/comments", new { date = Today, text = "hi" })).StatusCode);
        }

        As(client);
        foreach (var url in reads.Where(u => !u.Contains("meal-plans/active")))
            Assert.True(HttpStatusCode.OK == (await _client.GetAsync(url)).StatusCode, $"{url} as client");
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/meal-plans/active")).StatusCode);
    }

    [Fact]
    public async Task MealPlan_PrescribedByNutritionist_IsReadAndActivatedByClient()
    {
        var (pro, client) = await LinkedPairAsync();
        As(pro);
        var food = await CreateFoodAsync("Prescribed Food", 200, 10, 30, 5);

        var create = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new
        {
            name = "Cut plan",
            targetUserId = client.UserId,
            items = new[] { new { mealSlot = "lunch", foodId = food.Id, quantityGramsOrMl = 100m } }
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var plan = (await create.Content.ReadFromJsonAsync<MealPlanPayload>())!;
        Assert.Equal(pro.UserId, plan.PrescribedByUserId);

        var seenByPro = await _client.GetFromJsonAsync<MealPlanPayload[]>($"/api/nutrition/users/{client.UserId}/meal-plans");
        Assert.Contains(seenByPro!, p => p.Id == plan.Id);

        As(client);
        var mine = await _client.GetFromJsonAsync<MealPlanPayload[]>("/api/nutrition/meal-plans");
        Assert.Contains(mine!, p => p.Id == plan.Id && !p.IsActive);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/nutrition/meal-plans/active")).StatusCode);

        var activate = await _client.PostAsync($"/api/nutrition/meal-plans/{plan.Id}/activate?date={Today:yyyy-MM-dd}", null);
        Assert.Equal(HttpStatusCode.OK, activate.StatusCode);

        var active = await _client.GetFromJsonAsync<MealPlanPayload>("/api/nutrition/meal-plans/active");
        Assert.Equal(plan.Id, active!.Id);
        var byId = await _client.GetFromJsonAsync<MealPlanPayload>($"/api/nutrition/meal-plans/{plan.Id}");
        Assert.Equal("Cut plan", byId!.Name);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync("/api/nutrition/meal-plans/000000000000000000000000")).StatusCode);

        As(pro);
        var activeSeenByPro = await _client.GetFromJsonAsync<MealPlanPayload>($"/api/nutrition/users/{client.UserId}/meal-plans/active");
        Assert.Equal(plan.Id, activeSeenByPro!.Id);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/meal-plans/{plan.Id}")).StatusCode);
    }

    [Fact]
    public async Task MealPlanTemplates_CrudAndAssign_RespectOwnershipCapabilityAndLink()
    {
        var (pro, client) = await LinkedPairAsync();
        var otherPro = await SeedUserAsync(nutritionist: true);
        var plain = await SeedUserAsync();
        As(pro);
        var food = await CreateFoodAsync("Template Food", 100, 5, 20, 2);
        var body = new { name = "Base plan", notes = "n", items = new[] { new { mealSlot = "Breakfast", foodId = food.Id, quantityGramsOrMl = 80m } } };

        As(plain);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync("/api/nutrition/meal-plan-templates", body)).StatusCode);

        As(pro);
        var create = await _client.PostAsJsonAsync("/api/nutrition/meal-plan-templates", body);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var template = (await create.Content.ReadFromJsonAsync<TemplatePayload>())!;
        Assert.Equal("breakfast", template.Items[0].MealSlot);

        Assert.Contains((await _client.GetFromJsonAsync<TemplatePayload[]>("/api/nutrition/meal-plan-templates"))!, t => t.Id == template.Id);
        Assert.Equal("Base plan", (await _client.GetFromJsonAsync<TemplatePayload>($"/api/nutrition/meal-plan-templates/{template.Id}"))!.Name);

        var update = await _client.PutAsJsonAsync($"/api/nutrition/meal-plan-templates/{template.Id}",
            new { name = "Renamed", items = new[] { new { mealSlot = "dinner", foodId = food.Id, quantityGramsOrMl = 120m } } });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        Assert.Equal("Renamed", (await update.Content.ReadFromJsonAsync<TemplatePayload>())!.Name);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PutAsJsonAsync($"/api/nutrition/meal-plan-templates/{template.Id}", new { name = "", items = Array.Empty<object>() })).StatusCode);

        As(otherPro);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.GetAsync($"/api/nutrition/meal-plan-templates/{template.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.DeleteAsync($"/api/nutrition/meal-plan-templates/{template.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/nutrition/meal-plan-templates/{template.Id}/assign/{client.UserId}", new { })).StatusCode);

        As(pro);
        var stranger = await SeedUserAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await _client.PostAsJsonAsync($"/api/nutrition/meal-plan-templates/{template.Id}/assign/{stranger.UserId}", new { })).StatusCode);

        var assign = await _client.PostAsJsonAsync($"/api/nutrition/meal-plan-templates/{template.Id}/assign/{client.UserId}", new { planName = "Week 1" });
        Assert.Equal(HttpStatusCode.Created, assign.StatusCode);
        var plan = (await assign.Content.ReadFromJsonAsync<MealPlanPayload>())!;
        Assert.Equal("Week 1", plan.Name);
        Assert.Equal(pro.UserId, plan.PrescribedByUserId);
        Assert.False(plan.IsActive);

        As(client);
        Assert.Contains((await _client.GetFromJsonAsync<MealPlanPayload[]>("/api/nutrition/meal-plans"))!, p => p.Id == plan.Id);

        As(pro);
        Assert.Equal(HttpStatusCode.OK, (await _client.DeleteAsync($"/api/nutrition/meal-plan-templates/{template.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/nutrition/meal-plan-templates/{template.Id}")).StatusCode);
    }

    [Fact]
    public async Task ClientsAdherence_SummarisesDiaryAgainstPrescribedGoal()
    {
        var (pro, client) = await LinkedPairAsync();
        As(pro);
        var food = await CreateFoodAsync("Adherence Food", 200, 10, 30, 5);

        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync($"/api/nutrition/users/{client.UserId}/goal",
            new { goal = new { kcal = 200, proteinG = 10, carbG = 30, fatG = 5 } })).StatusCode);

        As(client);
        var add = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = Guid.NewGuid().ToString("N")[..24], date = Today, mealSlot = "lunch", foodId = food.Id, quantityGramsOrMl = 100m
        });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);

        As(pro);
        var adherence = await _client.GetFromJsonAsync<Page<AdherencePayload>>("/api/nutrition/clients/adherence?days=7");
        var item = Assert.Single(adherence!.Items, a => a.ClientUserId == client.UserId);
        Assert.Equal(7, item.Days);
        Assert.Equal(1, item.DaysLogged);
        Assert.Equal(1, item.DaysWithinGoal);
        Assert.Equal(200, item.Prescribed!.Kcal);
        Assert.Equal(200, item.AverageConsumed!.Kcal);
        Assert.Equal(Today, item.LastLoggedDate);

        Assert.Equal(HttpStatusCode.BadRequest, (await _client.GetAsync("/api/nutrition/clients/adherence?days=0")).StatusCode);
    }

    [Fact]
    public async Task Restrictions_AreEditedByClientAndByNutritionist_AndShownInProfile()
    {
        var (pro, client) = await LinkedPairAsync();

        As(client);
        var own = await _client.PutAsJsonAsync("/api/nutrition/profile/restrictions", new { restrictions = "vegetarian", allergies = "peanut" });
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        var profile = await _client.GetFromJsonAsync<ProfilePayload>("/api/nutrition/profile");
        Assert.Equal("vegetarian", profile!.Restrictions);

        As(pro);
        var byPro = await _client.PutAsJsonAsync($"/api/nutrition/users/{client.UserId}/restrictions", new { restrictions = "lactose free", allergies = "shellfish" });
        Assert.Equal(HttpStatusCode.OK, byPro.StatusCode);
        var seenByPro = await _client.GetFromJsonAsync<ProfilePayload>($"/api/nutrition/users/{client.UserId}/profile");
        Assert.Equal("shellfish", seenByPro!.Allergies);

        As(client);
        Assert.Equal("lactose free", (await _client.GetFromJsonAsync<ProfilePayload>("/api/nutrition/profile"))!.Restrictions);
        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PutAsJsonAsync("/api/nutrition/profile/restrictions", new { restrictions = new string('x', 1001) })).StatusCode);
    }

    [Fact]
    public async Task Measurements_AreRecordedByNutritionist_AndReadByClient()
    {
        var (pro, client) = await LinkedPairAsync();

        As(pro);
        var create = await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/measurements", new
        {
            date = Today, weightKg = 82.5, heightCm = 178, bodyFatPercent = 18.2, waistCm = 90, hipCm = 100, notes = "first visit"
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var created = (await create.Content.ReadFromJsonAsync<MeasurementPayload>())!;
        Assert.Equal(pro.UserId, created.RecordedByUserId);

        Assert.Equal(HttpStatusCode.BadRequest,
            (await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/measurements", new { date = Today })).StatusCode);

        As(client);
        var list = await _client.GetFromJsonAsync<Page<MeasurementPayload>>($"/api/nutrition/users/{client.UserId}/measurements?from={Today.AddDays(-1):yyyy-MM-dd}&to={Today:yyyy-MM-dd}");
        var item = Assert.Single(list!.Items);
        Assert.Equal(82.5m, item.WeightKg);
        Assert.Equal("first visit", item.Notes);
    }

    [Fact]
    public async Task DiaryComments_AreWrittenByNutritionist_AndReadByClient()
    {
        var (pro, client) = await LinkedPairAsync();
        As(pro);
        var food = await CreateFoodAsync("Commented Food", 100, 5, 10, 1);

        As(client);
        var entryId = Guid.NewGuid().ToString("N")[..24];
        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new { id = entryId, date = Today, mealSlot = "lunch", foodId = food.Id, quantityGramsOrMl = 100m });

        As(pro);
        var day = await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/diary/comments", new { date = Today, text = "Great day" });
        Assert.Equal(HttpStatusCode.Created, day.StatusCode);
        var onEntry = await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/diary/comments", new { date = Today, text = "More veggies", entryId });
        Assert.Equal(HttpStatusCode.Created, onEntry.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/diary/comments", new { date = Today, text = "x", entryId = "missing" })).StatusCode);

        As(client);
        var comments = (await _client.GetFromJsonAsync<Page<CommentPayload>>($"/api/nutrition/users/{client.UserId}/diary/comments?date={Today:yyyy-MM-dd}"))!.Items;
        Assert.Equal(["Great day", "More veggies"], comments!.Select(c => c.Text).ToArray());
        Assert.All(comments!, c => Assert.Equal(pro.UserId, c.AuthorUserId));
        Assert.Equal(entryId, comments![1].EntryId);

        Assert.Equal(HttpStatusCode.Forbidden,
            (await _client.PostAsJsonAsync($"/api/nutrition/users/{client.UserId}/diary/comments", new { date = Today, text = "self" })).StatusCode);
    }

    [Fact]
    public async Task Fasting_IsReadOnlyForNutritionist()
    {
        var (pro, client) = await LinkedPairAsync();

        As(client);
        Assert.Equal(HttpStatusCode.OK, (await _client.PutAsJsonAsync("/api/nutrition/fasting/agenda",
            new { protocol = "16:8", eatingStartMinutes = 720, timeZone = "America/Sao_Paulo" })).StatusCode);

        As(pro);
        var snapshot = await _client.GetFromJsonAsync<FastingPayload>($"/api/nutrition/users/{client.UserId}/fasting");
        Assert.Equal("16:8", snapshot!.Agenda!.Protocol);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/nutrition/users/{client.UserId}/fasting/history")).StatusCode);
    }

    private async Task<(TestUser Pro, TestUser Client)> LinkedPairAsync()
    {
        var pro = await SeedUserAsync(nutritionist: true);
        var client = await SeedUserAsync();
        await LinkAsync(pro, client);
        return (pro, client);
    }

    private async Task LinkAsync(TestUser pro, TestUser client)
    {
        As(pro);
        var invite = await _client.PostAsync("/api/nutrition/clients/invites", null);
        Assert.Equal(HttpStatusCode.OK, invite.StatusCode);
        var token = (await invite.Content.ReadFromJsonAsync<InvitePayload>())!.Token;

        As(client);
        var accept = await _client.PostAsJsonAsync("/api/nutrition/clients/invites/accept", new { token });
        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        Assert.Equal(pro.UserId, (await accept.Content.ReadFromJsonAsync<AcceptPayload>())!.NutritionistUserId);
    }

    private async Task<FoodPayload> CreateFoodAsync(string name, int kcal, int protein, int carb, int fat)
    {
        var response = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"{name}-{Guid.NewGuid():N}",
            barcode = $"789{Guid.NewGuid():N}"[..13],
            macrosPer100 = new { kcal, proteinG = protein, carbG = carb, fatG = fat }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FoodPayload>())!;
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

        return new TestUser(user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record Page<T>(T[] Items, string? NextCursor);
    private sealed record TestUser(int UserId, string Token);
    private sealed record InvitePayload(string Token);
    private sealed record AcceptPayload(int NutritionistUserId);
    private sealed record ClientPayload(int ClientUserId, string? Name);
    private sealed record NutritionistPayload(int NutritionistUserId, string? Name);
    private sealed record MacroPayload(int Kcal, int ProteinG, int CarbG, int FatG);
    private sealed record FoodPayload(string Id, string Name);
    private sealed record MealPlanItemPayload(string MealSlot, string FoodId, decimal QuantityGramsOrMl);
    private sealed record MealPlanPayload(string Id, string Name, bool IsActive, int? PrescribedByUserId, MealPlanItemPayload[] Items);
    private sealed record TemplatePayload(string Id, string Name, string? Notes, MealPlanItemPayload[] Items);
    private sealed record AdherencePayload(int ClientUserId, int Days, int DaysLogged, int DaysWithinGoal, MacroPayload? Prescribed, MacroPayload? AverageConsumed, DateOnly? LastLoggedDate);
    private sealed record ProfilePayload(string? Restrictions, string? Allergies);
    private sealed record MeasurementPayload(int Id, DateOnly Date, decimal? WeightKg, string? Notes, int RecordedByUserId);
    private sealed record CommentPayload(int Id, string? EntryId, int AuthorUserId, string Text);
    private sealed record FastingAgendaPayload(string Protocol);
    private sealed record FastingPayload(FastingAgendaPayload? Agenda);
}
