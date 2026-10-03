using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Clients.ClientsAdherence;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Professional;

public class NutritionAdherenceTests
{
    private static readonly DateOnly Today = new(2026, 10, 3);
    private static readonly MacroValueObject Goal = new() { Kcal = 2000, ProteinG = 150, CarbG = 200, FatG = 60 };

    private static DiaryDay Day(int userId, DateOnly date, int kcal, int p, int c, int f) => new()
    {
        UserId = userId,
        Date = date,
        Entries =
        [
            new DiaryEntry
            {
                Id = Guid.NewGuid().ToString("N")[..24], MealSlot = "lunch", FoodId = "food", QuantityGramsOrMl = 100,
                ComputedMacros = new MacroValueObject { Kcal = kcal, ProteinG = p, CarbG = c, FatG = f }
            }
        ]
    };

    [Fact]
    public void Calculate_CountsDaysWithinToleranceAndAveragesConsumed()
    {
        var days = new[]
        {
            Day(1, Today, 2000, 150, 200, 60),
            Day(1, Today.AddDays(-1), 1000, 70, 100, 30),
            new DiaryDay { UserId = 1, Date = Today.AddDays(-2) }
        };

        var result = NutritionAdherenceCalculator.Calculate(days, Goal);

        Assert.Equal(2, result.DaysLogged);
        Assert.Equal(1, result.DaysWithinGoal);
        Assert.Equal(1500, result.AverageConsumed!.Kcal);
        Assert.Equal(110, result.AverageConsumed.ProteinG);
    }

    [Fact]
    public void Calculate_WithoutGoalOrLogs_HasNoDaysWithinGoal()
    {
        Assert.Equal(0, NutritionAdherenceCalculator.Calculate([Day(1, Today, 2000, 150, 200, 60)], null).DaysWithinGoal);

        var empty = NutritionAdherenceCalculator.Calculate([], Goal);
        Assert.Equal(0, empty.DaysLogged);
        Assert.Null(empty.AverageConsumed);
    }

    [Fact]
    public async Task Handler_WithoutNutritionCapability_Returns403()
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(true, false));

        var handler = new GetClientsAdherenceHandler(
            capabilities.Object, new Mock<IProfessionalClientRelationshipRepository>().Object,
            NutritionProfessionalTestSupport.Users().Object, NutritionProfessionalTestSupport.NewDb());

        var result = await handler.HandleAsync(7, 1, default, Today);

        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(91)]
    public async Task Handler_WithInvalidDays_Returns400(int days)
    {
        var handler = BuildHandler(NutritionProfessionalTestSupport.NewDb(), []);

        var result = await handler.HandleAsync(days, 1, default, Today);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Handler_SummarisesEachClientInTheWindowAndReportsLastRecordOutsideIt()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        db.Profiles.Add(new NutritionProfile { UserId = 10, ActiveGoal = Goal });
        db.DiaryDays.Add(Day(10, Today, 2000, 150, 200, 60));
        db.DiaryDays.Add(Day(10, Today.AddDays(-3), 500, 10, 10, 10));
        db.DiaryDays.Add(Day(10, Today.AddDays(-30), 2000, 150, 200, 60));
        db.DiaryDays.Add(Day(11, Today.AddDays(-40), 1800, 100, 100, 50));
        db.DiaryDays.Add(Day(99, Today, 2000, 150, 200, 60));
        await db.SaveChangesAsync();

        var result = await BuildHandler(db, [10, 11, 12]).HandleAsync(null, 1, default, Today);

        Assert.True(result.IsSuccess);
        var byClient = result.Value!.ToDictionary(r => r.ClientUserId);
        Assert.Equal(3, byClient.Count);

        var ana = byClient[10];
        Assert.Equal("Client 10", ana.Name);
        Assert.Equal(7, ana.Days);
        Assert.Equal(2, ana.DaysLogged);
        Assert.Equal(1, ana.DaysWithinGoal);
        Assert.Equal(2000, ana.Prescribed!.Kcal);
        Assert.Equal(1250, ana.AverageConsumed!.Kcal);
        Assert.Equal(Today, ana.LastLoggedDate);

        Assert.Equal(0, byClient[11].DaysLogged);
        Assert.Null(byClient[11].AverageConsumed);
        Assert.Equal(Today.AddDays(-40), byClient[11].LastLoggedDate);

        Assert.Null(byClient[12].LastLoggedDate);
    }

    private static GetClientsAdherenceHandler BuildHandler(ShapeUp.Features.Nutrition.Infrastructure.Data.NutritionDbContext db, int[] clientIds)
    {
        var capabilities = new Mock<IProfessionalCapabilityService>();
        capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        var relationships = new Mock<IProfessionalClientRelationshipRepository>();
        relationships.Setup(r => r.ListActiveByProfessionalAsync(1, "Nutrition", default))
            .ReturnsAsync(clientIds.Select(id => new ProfessionalClientRelationship
            {
                ProfessionalUserId = 1, ClientUserId = id, RelationshipType = "Nutrition", StartedAt = DateTime.UtcNow
            }).ToList());

        return new GetClientsAdherenceHandler(
            capabilities.Object, relationships.Object,
            NutritionProfessionalTestSupport.Users(clientIds.Select(id => (id, $"Client {id}")).ToArray()).Object, db);
    }
}
