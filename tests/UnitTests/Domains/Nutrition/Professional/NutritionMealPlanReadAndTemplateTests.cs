using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.MealPlans.GetActiveMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlanById;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlans;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.MealPlanTemplates.AssignMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.CreateMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.DeleteMealPlanTemplate;
using ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplateById;
using ShapeUp.Features.Nutrition.MealPlanTemplates.GetMealPlanTemplates;
using ShapeUp.Features.Nutrition.MealPlanTemplates.UpdateMealPlanTemplate;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Professional;

public class NutritionMealPlanReadAndTemplateTests
{
    private const int Pro = 1;
    private const int Client = 2;

    private readonly Mock<IMealPlanRepository> _plans = new();
    private readonly Mock<IMealPlanTemplateRepository> _templates = new();
    private readonly Mock<IFoodRepository> _foods = new();
    private readonly Mock<IProfessionalCapabilityService> _capabilities = new();

    public NutritionMealPlanReadAndTemplateTests()
    {
        _foods.Setup(f => f.GetByIdAsync("food-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FoodDocument { Id = "food-1", Name = "Rice", MacrosPer100 = new MacroValueObject() });
        _capabilities.Setup(c => c.GetAsync(Pro, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        _capabilities.Setup(c => c.GetAsync(Client, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, false));
    }

    private static MealPlanDocument Plan(string id, int userId, bool active = false) => new()
    {
        Id = id, UserId = userId, Name = $"Plan {id}", IsActive = active, PrescribedByUserId = Pro,
        Items = [new MealPlanItemDocument { MealSlot = "lunch", FoodId = "food-1", QuantityGramsOrMl = 100 }]
    };

    private static SaveMealPlanTemplateCommand Save(string name = "Cutting") =>
        new(name, [new MealPlanItemInputDto("Lunch", "food-1", 150m)], "notes");

    [Fact]
    public async Task ReadPlans_ClientReadsOwn_LinkedProfessionalReadsClient_StrangerGets403()
    {
        _plans.Setup(r => r.GetByUserAsync(Client, default)).ReturnsAsync([Plan("p1", Client, true), Plan("p2", Client)]);
        _plans.Setup(r => r.GetActiveAsync(Client, default)).ReturnsAsync(Plan("p1", Client, true));
        _plans.Setup(r => r.GetByIdAsync("p1", default)).ReturnsAsync(Plan("p1", Client, true));
        var policy = NutritionProfessionalTestSupport.Policy((Pro, Client)).Object;
        var list = new GetMealPlansHandler(_plans.Object, policy);
        var active = new GetActiveMealPlanHandler(_plans.Object, policy);
        var byId = new GetMealPlanByIdHandler(_plans.Object, policy);

        Assert.Equal(2, (await list.HandleAsync(Client, Client, default)).Value!.Length);
        Assert.Equal(2, (await list.HandleAsync(Pro, Client, default)).Value!.Length);
        Assert.Equal("p1", (await active.HandleAsync(Pro, Client, default)).Value!.Id);
        Assert.Equal("p1", (await byId.HandleAsync("p1", Client, Client, default)).Value!.Id);
        Assert.Equal(403, (await list.HandleAsync(9, Client, default)).Error!.StatusCode);
        Assert.Equal(403, (await active.HandleAsync(9, Client, default)).Error!.StatusCode);
        Assert.Equal(403, (await byId.HandleAsync("p1", 9, Client, default)).Error!.StatusCode);
    }

    [Fact]
    public async Task ReadPlans_NoActivePlanOrPlanOfAnotherUser_Returns404()
    {
        _plans.Setup(r => r.GetActiveAsync(Client, default)).ReturnsAsync((MealPlanDocument?)null);
        _plans.Setup(r => r.GetByIdAsync("other", default)).ReturnsAsync(Plan("other", 77));
        var policy = NutritionProfessionalTestSupport.Policy().Object;

        Assert.Equal(404, (await new GetActiveMealPlanHandler(_plans.Object, policy).HandleAsync(Client, Client, default)).Error!.StatusCode);
        Assert.Equal(404, (await new GetMealPlanByIdHandler(_plans.Object, policy).HandleAsync("other", Client, Client, default)).Error!.StatusCode);
        Assert.Equal(404, (await new GetMealPlanByIdHandler(_plans.Object, policy).HandleAsync("missing", Client, Client, default)).Error!.StatusCode);
    }

    [Fact]
    public async Task CreateTemplate_WithoutNutritionCapability_Returns403()
    {
        var handler = new CreateMealPlanTemplateHandler(_templates.Object, _foods.Object, _capabilities.Object, new SaveMealPlanTemplateCommandValidator());

        var result = await handler.HandleAsync(Save(), Client, default);

        Assert.Equal(403, result.Error!.StatusCode);
        _templates.Verify(t => t.CreateAsync(It.IsAny<MealPlanTemplateDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateTemplate_Persists_NormalizesSlot_AndRejectsUnknownFood()
    {
        MealPlanTemplateDocument? stored = null;
        _templates.Setup(t => t.CreateAsync(It.IsAny<MealPlanTemplateDocument>(), default))
            .Callback<MealPlanTemplateDocument, CancellationToken>((d, _) => stored = d).Returns(Task.CompletedTask);
        var handler = new CreateMealPlanTemplateHandler(_templates.Object, _foods.Object, _capabilities.Object, new SaveMealPlanTemplateCommandValidator());

        var created = await handler.HandleAsync(Save(), Pro, default);
        var unknownFood = await handler.HandleAsync(new SaveMealPlanTemplateCommand("X", [new MealPlanItemInputDto("lunch", "nope", 1m)]), Pro, default);
        var invalid = await handler.HandleAsync(new SaveMealPlanTemplateCommand("", []), Pro, default);

        Assert.True(created.IsSuccess);
        Assert.Equal(Pro, stored!.CreatedByUserId);
        Assert.Equal("lunch", stored.Items[0].MealSlot);
        Assert.Equal(404, unknownFood.Error!.StatusCode);
        Assert.Equal(400, invalid.Error!.StatusCode);
    }

    [Fact]
    public async Task TemplateReadUpdateDelete_OnlyForTheOwner()
    {
        var template = new MealPlanTemplateDocument { Id = "t1", CreatedByUserId = Pro, Name = "Old", Items = Plan("x", 0).Items };
        _templates.Setup(t => t.GetByIdAsync("t1", default)).ReturnsAsync(template);
        _templates.Setup(t => t.GetByCreatorAsync(Pro, default)).ReturnsAsync([template]);
        _templates.Setup(t => t.GetByCreatorAsync(9, default)).ReturnsAsync([]);

        Assert.Single((await new GetMealPlanTemplatesHandler(_templates.Object).HandleAsync(Pro, default)).Value!);
        Assert.Empty((await new GetMealPlanTemplatesHandler(_templates.Object).HandleAsync(9, default)).Value!);
        Assert.True((await new GetMealPlanTemplateByIdHandler(_templates.Object).HandleAsync("t1", Pro, default)).IsSuccess);
        Assert.Equal(403, (await new GetMealPlanTemplateByIdHandler(_templates.Object).HandleAsync("t1", 9, default)).Error!.StatusCode);
        Assert.Equal(404, (await new GetMealPlanTemplateByIdHandler(_templates.Object).HandleAsync("zz", Pro, default)).Error!.StatusCode);

        var update = new UpdateMealPlanTemplateHandler(_templates.Object, _foods.Object, new SaveMealPlanTemplateCommandValidator());
        Assert.Equal(403, (await update.HandleAsync("t1", Save("New"), 9, default)).Error!.StatusCode);
        var updated = await update.HandleAsync("t1", Save("New"), Pro, default);
        Assert.Equal("New", updated.Value!.Name);
        _templates.Verify(t => t.UpdateAsync(template, default), Times.Once);

        var delete = new DeleteMealPlanTemplateHandler(_templates.Object);
        Assert.Equal(403, (await delete.HandleAsync("t1", 9, default)).Error!.StatusCode);
        _templates.Verify(t => t.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.True((await delete.HandleAsync("t1", Pro, default)).IsSuccess);
        _templates.Verify(t => t.DeleteAsync("t1", default), Times.Once);
    }

    [Fact]
    public async Task Assign_CreatesInactivePlanForClientPrescribedByProfessional()
    {
        var template = new MealPlanTemplateDocument { Id = "t1", CreatedByUserId = Pro, Name = "Cutting", Items = Plan("x", 0).Items };
        _templates.Setup(t => t.GetByIdAsync("t1", default)).ReturnsAsync(template);
        MealPlanDocument? stored = null;
        _plans.Setup(p => p.CreateAsync(It.IsAny<MealPlanDocument>(), default))
            .Callback<MealPlanDocument, CancellationToken>((d, _) => stored = d).Returns(Task.CompletedTask);
        var handler = new AssignMealPlanTemplateHandler(_templates.Object, _plans.Object, NutritionProfessionalTestSupport.Policy((Pro, Client)).Object);

        var result = await handler.HandleAsync("t1", Client, new AssignMealPlanTemplateCommand("Week 1"), Pro, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(Client, stored!.UserId);
        Assert.Equal(Pro, stored.PrescribedByUserId);
        Assert.False(stored.IsActive);
        Assert.Equal("Week 1", stored.Name);
        Assert.NotEqual("t1", stored.Id);
        Assert.Single(stored.Items);
    }

    [Fact]
    public async Task Assign_WithoutLinkOrNotOwner_Returns403AndCreatesNothing()
    {
        _templates.Setup(t => t.GetByIdAsync("t1", default))
            .ReturnsAsync(new MealPlanTemplateDocument { Id = "t1", CreatedByUserId = Pro, Name = "T" });
        var handler = new AssignMealPlanTemplateHandler(_templates.Object, _plans.Object, NutritionProfessionalTestSupport.Policy().Object);

        var noLink = await handler.HandleAsync("t1", Client, new AssignMealPlanTemplateCommand(), Pro, default);
        var notOwner = await handler.HandleAsync("t1", 9, new AssignMealPlanTemplateCommand(), 9, default);
        var missing = await handler.HandleAsync("zz", Client, new AssignMealPlanTemplateCommand(), Pro, default);

        Assert.Equal(403, noLink.Error!.StatusCode);
        Assert.Equal(403, notOwner.Error!.StatusCode);
        Assert.Equal(404, missing.Error!.StatusCode);
        _plans.Verify(p => p.CreateAsync(It.IsAny<MealPlanDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
