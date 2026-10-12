namespace IntegrationTests.Domains.Nutrition.Endpoints;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Infrastructure;

[Collection("SQL Server Write Operations")]
public sealed class UserMealsEndpointsIntegrationTests(SqlServerFixture fixture) : IAsyncLifetime
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
    public async Task SaveMeal_WithAnyName_ReturnsItInTheList()
    {
        await AuthorizeNewUserAsync();

        var save = await Put("aaaaaaaaaaaaaaaaaaaaaaa1", new
        {
            name = "Pré-treino de pernas",
            time = "06:30",
            weekDays = new[] { 1, 3, 5 },
            targetProteinG = 20,
            itemsJson = "[{\"id\":\"x\"}]"
        });
        Assert.Equal(HttpStatusCode.OK, save.StatusCode);

        var list = await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals");
        var meal = Assert.Single(list!);
        Assert.Equal("Pré-treino de pernas", meal.Name);
        Assert.Equal("06:30", meal.Time);
        Assert.Equal([1, 3, 5], meal.WeekDays);
        Assert.Equal(20, meal.TargetProteinG);
        Assert.Equal("[{\"id\":\"x\"}]", meal.ItemsJson);
    }

    [Fact]
    public async Task SaveMeal_WithoutDaysAndTime_MeansEveryDayWithoutReminder()
    {
        await AuthorizeNewUserAsync();

        await Put("aaaaaaaaaaaaaaaaaaaaaaa2", new { name = "Ceia" });

        var meal = Assert.Single((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
        Assert.Empty(meal.WeekDays);
        Assert.Null(meal.Time);
    }

    [Fact]
    public async Task SaveMeal_WithStartDate_KeepsItAndNullMeansRetroactive()
    {
        await AuthorizeNewUserAsync();

        await Put("aaaaaaaaaaaaaaaaaaaaaab1", new { name = "A partir de hoje", startDate = "2026-10-07" });
        await Put("aaaaaaaaaaaaaaaaaaaaaab2", new { name = "Retroativa" });

        var list = await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals");
        Assert.Equal(new DateOnly(2026, 10, 7), list!.Single(m => m.Name == "A partir de hoje").StartDate);
        Assert.Null(list!.Single(m => m.Name == "Retroativa").StartDate);

        // Making it retroactive later clears the date.
        await Put("aaaaaaaaaaaaaaaaaaaaaab1", new { name = "A partir de hoje" });
        var after = await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals");
        Assert.Null(after!.Single(m => m.Name == "A partir de hoje").StartDate);
    }

    [Fact]
    public async Task SaveMeal_OnlyToday_KeepsStartAndEndOnTheSameDay()
    {
        await AuthorizeNewUserAsync();

        await Put("aaaaaaaaaaaaaaaaaaaaaae1",
            new { name = "Só hoje", startDate = "2026-10-07", endDate = "2026-10-07" });

        var meal = Assert.Single((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
        Assert.Equal(new DateOnly(2026, 10, 7), meal.StartDate);
        Assert.Equal(new DateOnly(2026, 10, 7), meal.EndDate);

        // Turning it into a regular meal later clears the end.
        await Put("aaaaaaaaaaaaaaaaaaaaaae1", new { name = "Agora fixa", startDate = "2026-10-07" });
        var after = Assert.Single((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
        Assert.Null(after.EndDate);
    }

    [Fact]
    public async Task SaveMeal_WithEndBeforeStart_ReturnsBadRequest()
    {
        await AuthorizeNewUserAsync();

        var response = await Put("aaaaaaaaaaaaaaaaaaaaaae2",
            new { name = "Invertida", startDate = "2026-10-07", endDate = "2026-10-06" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Batch_CarriesTheEndDate_AndRejectsEndBeforeStart()
    {
        await AuthorizeNewUserAsync();

        var ok = await Batch(new
        {
            upserts = new object[]
            {
                new { id = "aaaaaaaaaaaaaaaaaaaaaae3", name = "Temporária", startDate = "2026-10-07", endDate = "2026-10-07" }
            }
        });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var meal = Assert.Single((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
        Assert.Equal(new DateOnly(2026, 10, 7), meal.EndDate);

        var bad = await Batch(new
        {
            upserts = new object[]
            {
                new { id = "aaaaaaaaaaaaaaaaaaaaaae4", name = "Ruim", startDate = "2026-10-07", endDate = "2026-10-01" }
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, bad.StatusCode);
        Assert.Single((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
    }

    [Fact]
    public async Task SaveMeal_WhenRetried_IsIdempotentAndUpdates()
    {
        await AuthorizeNewUserAsync();

        await Put("aaaaaaaaaaaaaaaaaaaaaaa3", new { name = "Lanche" });
        await Put("aaaaaaaaaaaaaaaaaaaaaaa3", new { name = "Lanche da tarde", time = "16:00" });

        var meal = Assert.Single((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
        Assert.Equal("Lanche da tarde", meal.Name);
        Assert.Equal("16:00", meal.Time);
    }

    [Theory]
    [InlineData("{\"name\":\"\"}")]
    [InlineData("{\"name\":\"x\",\"time\":\"25:00\"}")]
    [InlineData("{\"name\":\"x\",\"weekDays\":[0]}")]
    [InlineData("{\"name\":\"x\",\"weekDays\":[8]}")]
    [InlineData("{\"name\":\"x\",\"itemsJson\":\"{}\"}")]
    [InlineData("{\"name\":\"x\",\"itemsJson\":\"not json\"}")]
    public async Task SaveMeal_WithInvalidBody_ReturnsBadRequest(string json)
    {
        await AuthorizeNewUserAsync();

        var response = await _client.PutAsync("/api/nutrition/meals/aaaaaaaaaaaaaaaaaaaaaaa4",
            new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task NewMeals_AreAppendedAtTheEnd()
    {
        await AuthorizeNewUserAsync();

        await Put("aaaaaaaaaaaaaaaaaaaaaaa5", new { name = "A" });
        await Put("aaaaaaaaaaaaaaaaaaaaaaa6", new { name = "B" });
        await Put("aaaaaaaaaaaaaaaaaaaaaaa7", new { name = "C" });

        Assert.Equal(["A", "B", "C"], await NamesAsync());
    }

    private Task<HttpResponseMessage> Batch(object body) =>
        _client.PostAsJsonAsync("/api/nutrition/meals/batch", body);

    private async Task<string[]> NamesAsync() =>
        (await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!.Select(m => m.Name).ToArray();

    [Fact]
    public async Task Batch_CreatesSeveralMealsInTheOrderSent()
    {
        await AuthorizeNewUserAsync();

        var response = await Batch(new
        {
            upserts = new object[]
            {
                new { id = "fffffffffffffffffffffff1", name = "Café", time = "07:00" },
                new { id = "fffffffffffffffffffffff2", name = "Almoço", weekDays = new[] { 1, 2 } },
                new { id = "fffffffffffffffffffffff3", name = "Jantar", startDate = "2026-10-07" },
            }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["Café", "Almoço", "Jantar"], await NamesAsync());
        var list = await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals");
        Assert.Equal("07:00", list![0].Time);
        Assert.Equal([1, 2], list[1].WeekDays);
        Assert.Equal(new DateOnly(2026, 10, 7), list[2].StartDate);
    }

    [Fact]
    public async Task Batch_ReorderOnly_NeedsJustTheIds()
    {
        await AuthorizeNewUserAsync();
        await Batch(new
        {
            upserts = new object[]
            {
                new { id = "fffffffffffffffffffffff4", name = "A" },
                new { id = "fffffffffffffffffffffff5", name = "B" },
                new { id = "fffffffffffffffffffffff6", name = "C" },
            }
        });

        var response = await Batch(new
        {
            order = new[] { "fffffffffffffffffffffff6", "fffffffffffffffffffffff4", "fffffffffffffffffffffff5" }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(["C", "A", "B"], await NamesAsync());
    }

    [Fact]
    public async Task Batch_DeletesAndChangesTogether_AndIgnoresMissingDeletes()
    {
        await AuthorizeNewUserAsync();
        await Batch(new
        {
            upserts = new object[]
            {
                new { id = "fffffffffffffffffffffff7", name = "A" },
                new { id = "fffffffffffffffffffffff8", name = "B" },
                new { id = "fffffffffffffffffffffff9", name = "C" },
            }
        });
        var body = new
        {
            upserts = new object[] { new { id = "fffffffffffffffffffffff9", name = "C renomeada" } },
            deleteIds = new[] { "fffffffffffffffffffffff7", "fffffffffffffffffffffffa" },
        };

        Assert.Equal(HttpStatusCode.OK, (await Batch(body)).StatusCode);
        Assert.Equal(["B", "C renomeada"], await NamesAsync());

        // A retry of the same batch (offline queue) changes nothing and does not fail.
        Assert.Equal(HttpStatusCode.OK, (await Batch(body)).StatusCode);
        Assert.Equal(["B", "C renomeada"], await NamesAsync());
    }

    [Fact]
    public async Task Batch_NewMealWithOrder_LandsWhereTheOrderPutsIt()
    {
        await AuthorizeNewUserAsync();
        await Batch(new
        {
            upserts = new object[]
            {
                new { id = "fffffffffffffffffffffffb", name = "A" },
                new { id = "fffffffffffffffffffffffc", name = "B" },
            }
        });

        await Batch(new
        {
            upserts = new object[] { new { id = "fffffffffffffffffffffffd", name = "No meio" } },
            order = new[] { "fffffffffffffffffffffffb", "fffffffffffffffffffffffd", "fffffffffffffffffffffffc" },
        });

        Assert.Equal(["A", "No meio", "B"], await NamesAsync());
    }

    [Fact]
    public async Task Batch_WithOneInvalidItem_ChangesNothing()
    {
        await AuthorizeNewUserAsync();
        await Put("aaaaaaaaaaaaaaaaaaaaaad1", new { name = "Existente" });

        var response = await Batch(new
        {
            upserts = new object[]
            {
                new { id = "aaaaaaaaaaaaaaaaaaaaaad2", name = "Válida" },
                new { id = "aaaaaaaaaaaaaaaaaaaaaad3", name = "Horário ruim", time = "25:99" },
            },
            deleteIds = new[] { "aaaaaaaaaaaaaaaaaaaaaad1" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(["Existente"], await NamesAsync());
    }

    [Fact]
    public async Task Batch_CannotTouchAnotherUsersMeal()
    {
        await AuthorizeNewUserAsync();
        await Put("aaaaaaaaaaaaaaaaaaaaaad4", new { name = "Só minha" });

        await AuthorizeNewUserAsync();
        var hijack = await Batch(new
        {
            upserts = new object[] { new { id = "aaaaaaaaaaaaaaaaaaaaaad4", name = "Invasor" } }
        });
        Assert.Equal(HttpStatusCode.Conflict, hijack.StatusCode);

        // Deleting someone else's id is a no-op for the caller.
        Assert.Equal(HttpStatusCode.OK,
            (await Batch(new { deleteIds = new[] { "aaaaaaaaaaaaaaaaaaaaaad4" } })).StatusCode);

        await AuthorizeNewUserAsync();
        Assert.Empty(await NamesAsync());
    }

    [Fact]
    public async Task Batch_RejectsRepeatedIdsAndDeleteOfAnUpsertedMeal()
    {
        await AuthorizeNewUserAsync();

        var repeated = await Batch(new
        {
            upserts = new object[]
            {
                new { id = "aaaaaaaaaaaaaaaaaaaaaad5", name = "X" },
                new { id = "aaaaaaaaaaaaaaaaaaaaaad5", name = "Y" },
            }
        });
        var both = await Batch(new
        {
            upserts = new object[] { new { id = "aaaaaaaaaaaaaaaaaaaaaad6", name = "X" } },
            deleteIds = new[] { "aaaaaaaaaaaaaaaaaaaaaad6" },
        });

        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, both.StatusCode);
    }

    [Fact]
    public async Task DeleteMeal_RemovesIt_AndUnknownReturnsNotFound()
    {
        await AuthorizeNewUserAsync();
        await Put("aaaaaaaaaaaaaaaaaaaaaaa8", new { name = "Jantar" });

        var delete = await _client.DeleteAsync("/api/nutrition/meals/aaaaaaaaaaaaaaaaaaaaaaa8");
        Assert.Equal(HttpStatusCode.OK, delete.StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);

        var again = await _client.DeleteAsync("/api/nutrition/meals/aaaaaaaaaaaaaaaaaaaaaaa8");
        Assert.Equal(HttpStatusCode.NotFound, again.StatusCode);
    }

    [Fact]
    public async Task Meals_AreIsolatedPerUser()
    {
        await AuthorizeNewUserAsync();
        await Put("aaaaaaaaaaaaaaaaaaaaaaa9", new { name = "Só minha" });

        await AuthorizeNewUserAsync();
        Assert.Empty((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
        Assert.Equal(HttpStatusCode.NotFound,
            (await _client.DeleteAsync("/api/nutrition/meals/aaaaaaaaaaaaaaaaaaaaaaa9")).StatusCode);
        // The id is taken by the other user: no hijack through upsert.
        Assert.Equal(HttpStatusCode.Conflict,
            (await Put("aaaaaaaaaaaaaaaaaaaaaaa9", new { name = "Invasor" })).StatusCode);
    }

    [Fact]
    public async Task DiaryEntry_WithMealId_GroupsByThatMealAndKeepsItsName()
    {
        await AuthorizeNewUserAsync();
        await Put("bbbbbbbbbbbbbbbbbbbbbbb1", new { name = "Pré-treino" });
        await Put("bbbbbbbbbbbbbbbbbbbbbbb2", new { name = "Pós-treino" });
        var food = await CreateFoodAsync();
        var date = new DateOnly(2026, 5, 20);

        foreach (var (entry, meal) in new[] { ("cccccccccccccccccccccc01", "bbbbbbbbbbbbbbbbbbbbbbb1"), ("cccccccccccccccccccccc02", "bbbbbbbbbbbbbbbbbbbbbbb2") })
        {
            var response = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
            {
                id = entry,
                date,
                mealId = meal,
                foodId = food,
                quantityGramsOrMl = 100m
            });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var day = await _client.GetFromJsonAsync<DayPayload>($"/api/nutrition/diary?date={date:yyyy-MM-dd}");
        Assert.Equal(2, day!.Meals.Length);
        Assert.Contains(day.Meals, m => m.MealName == "Pré-treino" && m.MealId == "bbbbbbbbbbbbbbbbbbbbbbb1");
        Assert.Contains(day.Meals, m => m.MealName == "Pós-treino" && m.MealId == "bbbbbbbbbbbbbbbbbbbbbbb2");

        // Deleting the meal does not rewrite history: the name stays on the entries.
        await _client.DeleteAsync("/api/nutrition/meals/bbbbbbbbbbbbbbbbbbbbbbb1");
        var after = await _client.GetFromJsonAsync<DayPayload>($"/api/nutrition/diary?date={date:yyyy-MM-dd}");
        Assert.Contains(after!.Meals, m => m.MealName == "Pré-treino");
    }

    [Fact]
    public async Task DiaryEntry_WithMealNotYetSynced_IsKeptWithTheNameTheClientSent()
    {
        await AuthorizeNewUserAsync();
        var food = await CreateFoodAsync();
        var date = new DateOnly(2026, 5, 21);

        // O PUT da refeição ainda está na fila offline: a entrada não pode ser perdida.
        var response = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "cccccccccccccccccccccc03",
            date,
            mealId = "ddddddddddddddddddddddd1",
            mealName = "Pré-treino",
            foodId = food,
            quantityGramsOrMl = 100m
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var day = await _client.GetFromJsonAsync<DayPayload>($"/api/nutrition/diary?date={date:yyyy-MM-dd}");
        var group = Assert.Single(day!.Meals);
        Assert.Equal("ddddddddddddddddddddddd1", group.MealId);
        Assert.Equal("Pré-treino", group.MealName);
    }

    [Fact]
    public async Task SubstituteEntry_AfterItsMealWasDeleted_StillWorks()
    {
        await AuthorizeNewUserAsync();
        await Put("bbbbbbbbbbbbbbbbbbbbbbb3", new { name = "Almoço leve" });
        var food = await CreateFoodAsync();
        var other = await CreateFoodAsync();
        var date = new DateOnly(2026, 5, 23);
        var add = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "cccccccccccccccccccccc05",
            date,
            mealId = "bbbbbbbbbbbbbbbbbbbbbbb3",
            foodId = food,
            quantityGramsOrMl = 100m
        });
        Assert.Equal(HttpStatusCode.OK, add.StatusCode);
        await _client.DeleteAsync("/api/nutrition/meals/bbbbbbbbbbbbbbbbbbbbbbb3");

        var substitute = await _client.PutAsJsonAsync("/api/nutrition/diary/entries/cccccccccccccccccccccc05/substitute",
            new { date, replacementFoodId = other, quantityGramsOrMl = 150m });

        Assert.Equal(HttpStatusCode.OK, substitute.StatusCode);
        var day = await _client.GetFromJsonAsync<DayPayload>($"/api/nutrition/diary?date={date:yyyy-MM-dd}");
        Assert.Equal("Almoço leve", Assert.Single(day!.Meals).MealName);
    }

    [Fact]
    public async Task SaveMeal_WithEmptyTime_MeansNoReminder()
    {
        await AuthorizeNewUserAsync();

        var response = await Put("aaaaaaaaaaaaaaaaaaaaaac1", new { name = "Sem horário", time = "" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var meal = Assert.Single((await _client.GetFromJsonAsync<MealPayload[]>("/api/nutrition/meals"))!);
        Assert.Null(meal.Time);
    }

    [Fact]
    public async Task SaveMeal_BeyondFiftyMeals_IsRejected()
    {
        await AuthorizeNewUserAsync();

        for (var i = 0; i < 50; i++)
        {
            var ok = await Put($"eeeeeeeeeeeeeeeeeeeeee{i:D2}", new { name = $"Refeição {i}" });
            Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        }

        var extra = await Put("eeeeeeeeeeeeeeeeeeeeeeff", new { name = "Demais" });
        Assert.Equal(HttpStatusCode.BadRequest, extra.StatusCode);

        // Alterar uma existente continua valendo no limite.
        var update = await Put("eeeeeeeeeeeeeeeeeeeeee00", new { name = "Renomeada" });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    [Fact]
    public async Task DiaryEntry_WithoutMealIdNorSlot_ReturnsBadRequest()
    {
        await AuthorizeNewUserAsync();
        var food = await CreateFoodAsync();

        var response = await _client.PostAsJsonAsync("/api/nutrition/diary/entries", new
        {
            id = "cccccccccccccccccccccc04",
            date = new DateOnly(2026, 5, 22),
            foodId = food,
            quantityGramsOrMl = 100m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<HttpResponseMessage> Put(string id, object body) =>
        _client.PutAsJsonAsync($"/api/nutrition/meals/{id}", body);

    private async Task<string> CreateFoodAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/nutrition/foods", new
        {
            name = $"Meal Rice-{Guid.NewGuid():N}",
            barcode = $"789{Guid.NewGuid():N}"[..13],
            macrosPer100 = new { kcal = 100, proteinG = 5, carbG = 20, fatG = 1 }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<FoodPayload>())!.Id;
    }

    private async Task AuthorizeNewUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    private sealed record MealPayload(
        string Id, string Name, string? Time, int[] WeekDays, int Order,
        int? TargetProteinG, int? TargetCarbG, int? TargetFatG, string? ItemsJson, DateOnly? StartDate, DateOnly? EndDate);

    private sealed record FoodPayload(string Id);
    private sealed record GroupPayload(string MealSlot, string? MealName, string? MealId);
    private sealed record DayPayload(GroupPayload[] Meals);
}
