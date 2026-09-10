namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using ShapeUp.Features.Nutrition.Shared.Abstractions;

[Collection("SQL Server Write Operations")]
public sealed class DiaryEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task GetDiaryDay_WhenNoEntriesExist_ReturnsEmptyDayWithZeroTotals()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var response = await _client.GetAsync("/api/nutrition/diary?date=2026-05-01");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var day = await response.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(new DateOnly(2026, 5, 1), day!.Date);
        Assert.Empty(day.Meals);
        Assert.Equal(0, day.Totals.Kcal);
    }

    [Fact]
    public async Task AddDiaryEntry_ShouldGroupByMealAndCalculateTotals()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Diary Rice", 200, 8, 40, 2);
        var date = new DateOnly(2026, 5, 2);

        var breakfast = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-breakfast-1",
            date,
            mealSlot = "breakfast",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        });
        Assert.Equal(HttpStatusCode.OK, breakfast.StatusCode);

        var lunch = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-lunch-1",
            date,
            mealSlot = "lunch",
            foodId = food.Id,
            quantityGramsOrMl = 50m
        });
        Assert.Equal(HttpStatusCode.OK, lunch.StatusCode);

        var day = await lunch.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(2, day!.Meals.Length);
        Assert.Equal(300, day.Totals.Kcal);
        Assert.Equal(12, day.Totals.ProteinG);
    }

    [Fact]
    public async Task AddDiaryEntry_WhenSameIdIsRetried_IsIdempotent()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Idempotent Beans", 90, 6, 15, 1);
        var date = new DateOnly(2026, 5, 3);
        var payload = new
        {
            id = "entry-idempotent-1",
            date,
            mealSlot = "dinner",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        };

        var first = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", payload);
        var second = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", payload);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var get = await _client.GetAsync("/api/nutrition/diary?date=2026-05-03");
        var day = await get.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Single(day!.Meals);
        Assert.Single(day.Meals[0].Items);
        Assert.Equal(90, day.Totals.Kcal);
    }

    [Fact]
    public async Task AddDiaryEntry_WhenOverrideIsActive_UsesOverrideMacros()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Override Pasta", 100, 10, 20, 5);
        await _client.PostAsJsonAsync(
            $"/api/nutrition/foods/{food.Id}/override",
            new { macrosPer100 = new { kcal = 140, proteinG = 12, carbG = 22, fatG = 6 } });

        var response = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-override-1",
            date = new DateOnly(2026, 5, 4),
            mealSlot = "lunch",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var day = await response.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(140, day!.Totals.Kcal);
        Assert.True(day.Meals[0].Items[0].UsesOverride);
    }

    [Fact]
    public async Task RemoveDiaryEntry_ShouldRecalculateDayTotals()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var food = await CreateFoodAsync("Removable Oats", 389, 17, 66, 7);
        var date = new DateOnly(2026, 5, 5);

        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-remove-1",
            date,
            mealSlot = "breakfast",
            foodId = food.Id,
            quantityGramsOrMl = 100m
        });

        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "entry-remove-2",
            date,
            mealSlot = "lunch",
            foodId = food.Id,
            quantityGramsOrMl = 50m
        });

        var remove = await _client.DeleteAsync($"/api/nutrition/diary/entries/entry-remove-1?date={date:yyyy-MM-dd}");
        Assert.Equal(HttpStatusCode.OK, remove.StatusCode);

        var day = await remove.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(195, day!.Totals.Kcal);
        Assert.Single(day.Meals[0].Items);
    }

    [Fact]
    public async Task SuggestSubstitutes_ShouldReturnCandidatesRankedByMacroDistance()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var original = await CreateFoodAsync("Original Rice", 100, 10, 20, 5);
        var closeMatch = await CreateFoodAsync("Close Rice", 110, 11, 21, 5);

        var date = new DateOnly(2026, 7, 1);
        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "substitute-entry-1",
            date,
            mealSlot = "lunch",
            foodId = original.Id,
            quantityGramsOrMl = 100m
        });

        var response = await _client.GetAsync($"/api/nutrition/diary/substitutes?date={date:yyyy-MM-dd}&entryId=substitute-entry-1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<SuggestSubstitutePayload>();
        Assert.NotNull(payload);
        var close = payload!.Suggestions.FirstOrDefault(x => x.FoodId == closeMatch.Id);
        Assert.NotNull(close);
        Assert.True(close!.Distance > 0);
    }

    [Fact]
    public async Task SubstituteDiaryItem_ShouldUpdateDayTotalsWithoutMutatingMealPlan()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        var planFood = await CreateFoodAsync("Plan Base", 100, 10, 20, 5);

        var createPlan = await _client.PostAsJsonAsync("/api/nutrition/meal-plans", new
        {
            name = "Substitute Plan",
            items = new[] { new { mealSlot = "dinner", foodId = planFood.Id, quantityGramsOrMl = 100m } }
        });
        var plan = await createPlan.Content.ReadFromJsonAsync<MealPlanPayload>();
        Assert.NotNull(plan);

        var date = new DateOnly(2026, 7, 2);
        await _client.PostAsync($"/api/nutrition/meal-plans/{plan!.Id}/activate?date={date:yyyy-MM-dd}", null);

        var getDay = await _client.GetAsync($"/api/nutrition/diary?date={date:yyyy-MM-dd}");
        var dayBefore = await getDay.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(dayBefore);
        var entryId = dayBefore!.Meals.Single().Items.Single().Id;

        var substitute = await _client.PutAsJsonAsync($"/api/nutrition/diary/entries/{entryId}/substitute", new
        {
            date,
            replacementFoodId = planFood.Id,
            quantityGramsOrMl = 200m
        });
        Assert.Equal(HttpStatusCode.OK, substitute.StatusCode);

        var day = await substitute.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(200, day!.Totals.Kcal);

        using var scope = _factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMealPlanRepository>();
        var storedPlan = await repository.GetByIdAsync(plan.Id, CancellationToken.None);
        Assert.NotNull(storedPlan);
        Assert.Equal(planFood.Id, storedPlan!.Items[0].FoodId);
        Assert.Equal(100m, storedPlan.Items[0].QuantityGramsOrMl);
    }

    [Fact]
    public async Task SubstituteDiaryItem_WithFreeChoiceAboveGoal_IsAccepted()
    {
        var auth = await SeedAuthorizedUserAsync();
        Authorize(auth.Token);

        await _client.PutAsJsonAsync("/api/nutrition/profile/goal", new
        {
            goal = new { kcal = 500, proteinG = 40, carbG = 50, fatG = 15 }
        });

        var heavy = await CreateFoodAsync("Heavy Meal", 900, 60, 90, 40);

        var date = new DateOnly(2026, 7, 3);
        await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "free-choice-entry",
            date,
            mealSlot = "dinner",
            foodId = heavy.Id,
            quantityGramsOrMl = 50m
        });

        var substitute = await _client.PutAsJsonAsync("/api/nutrition/diary/entries/free-choice-entry/substitute", new
        {
            date,
            replacementFoodId = heavy.Id,
            quantityGramsOrMl = 100m
        });

        Assert.Equal(HttpStatusCode.OK, substitute.StatusCode);
        var day = await substitute.Content.ReadFromJsonAsync<DiaryDayPayload>();
        Assert.NotNull(day);
        Assert.Equal(900, day!.Totals.Kcal);
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
        var food = await response.Content.ReadFromJsonAsync<FoodPayload>();
        Assert.NotNull(food);
        return food!;
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
    private sealed record MacroPayload(int Kcal, int ProteinG, int CarbG, int FatG);
    private sealed record DiaryEntryPayload(
        string Id,
        string MealSlot,
        string FoodId,
        bool UsesOverride,
        decimal QuantityGramsOrMl,
        MacroPayload ComputedMacros);
    private sealed record DiaryMealPayload(string MealSlot, DiaryEntryPayload[] Items);
    private sealed record DiaryDayPayload(DateOnly Date, DiaryMealPayload[] Meals, MacroPayload Totals);
    private sealed record FoodPayload(string Id, string Name, MacroPayload MacrosPer100);
    private sealed record SubstituteSuggestionPayload(string FoodId, string Name, double Distance, MacroPayload MacrosPer100);
    private sealed record SuggestSubstitutePayload(SubstituteSuggestionPayload[] Suggestions);
    private sealed record MealPlanItemPayload(string MealSlot, string FoodId, decimal QuantityGramsOrMl);
    private sealed record MealPlanPayload(string Id, string Name, int? PrescribedByRelationshipId, bool IsActive, MealPlanItemPayload[] Items);
}
