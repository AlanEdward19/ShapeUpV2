using Moq;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Nutrition.MealPlans;

public class NutritionLockGuardTests
{
    private const int Client = 5;
    private const int Nutri = 9;
    private readonly Mock<IProfessionalClientRelationshipRepository> _relationships = new();
    private readonly Mock<IMealPlanRepository> _plans = new();

    private NutritionLockGuard Guard() => new(_relationships.Object, _plans.Object);

    private void Linked(bool linked) =>
        _relationships.Setup(r => r.ListActiveByClientAsync(Client, "Nutrition", It.IsAny<CancellationToken>()))
            .ReturnsAsync(linked
                ? new List<ProfessionalClientRelationship> { new() { ProfessionalUserId = Nutri, ClientUserId = Client, RelationshipType = "Nutrition" } }
                : new List<ProfessionalClientRelationship>());

    private void Active(MealPlanDocument? plan) =>
        _plans.Setup(p => p.GetActiveAsync(Client, It.IsAny<CancellationToken>())).ReturnsAsync(plan);

    private static MealPlanDocument Prescribed(bool withItems, string id = "a") => new()
    {
        Id = id, UserId = Client, Name = "p", PrescribedByUserId = Nutri,
        Items = withItems ? [new MealPlanItemDocument { MealSlot = "lunch", FoodId = "f", QuantityGramsOrMl = 100 }] : [],
        Meals = withItems ? [] : [new MealPlanMealTargetDocument { MealSlot = "lunch", TargetCalories = 600 }]
    };

    [Fact]
    public async Task Goal_LinkedClientEditingOwn_IsLockedWith403AndStableCode()
    {
        Linked(true);
        var result = await Guard().EnsureGoalEditableAsync(Client, Client, default);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Equal("nutrition.goal.lockedByNutritionist", result.Error.Code);
    }

    [Fact]
    public async Task Goal_UnlinkedClient_IsAllowed()
    {
        Linked(false);
        Assert.True((await Guard().EnsureGoalEditableAsync(Client, Client, default)).IsSuccess);
    }

    [Fact]
    public async Task Goal_NutritionistSettingClientGoal_IsAllowedWithoutLookup()
    {
        var result = await Guard().EnsureGoalEditableAsync(Nutri, Client, default);

        Assert.True(result.IsSuccess);
        _relationships.Verify(r => r.ListActiveByClientAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Plan_LinkedClientWithFullPrescribedPlan_IsLocked()
    {
        Linked(true);
        Active(Prescribed(withItems: true));

        var result = await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, null, default);

        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Equal("nutrition.mealPlan.lockedByNutritionist", result.Error.Code);
    }

    [Fact]
    public async Task Plan_LinkedClientWithMacrosOnlyPrescribedPlan_CanStructure()
    {
        Linked(true);
        Active(Prescribed(withItems: false));

        Assert.True((await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, null, default)).IsSuccess);
    }

    [Fact]
    public async Task Plan_LinkedClientWithoutPrescribedPlan_CanStructure()
    {
        Linked(true);
        Active(null);
        Assert.True((await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, null, default)).IsSuccess);

        Active(new MealPlanDocument { Id = "mine", UserId = Client, Name = "own", PrescribedByUserId = null });
        Assert.True((await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, null, default)).IsSuccess);
    }

    [Fact]
    public async Task Plan_UnlinkedClient_IsNeverLocked()
    {
        Linked(false);
        Active(Prescribed(withItems: true));

        Assert.True((await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, null, default)).IsSuccess);
    }

    [Fact]
    public async Task Plan_NutritionistPrescribing_IsNeverLocked()
    {
        Assert.True((await Guard().EnsureMealPlanStructureEditableAsync(Nutri, Client, null, default)).IsSuccess);
    }

    [Fact]
    public async Task Plan_ActivatingPrescribedPlanItself_IsAllowed()
    {
        Linked(true);
        Active(Prescribed(withItems: true, id: "a"));

        Assert.True((await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, "a", default)).IsSuccess);
    }

    [Fact]
    public async Task Plan_ActivatingAnotherPlanTheNutritionistSent_IsAllowed()
    {
        Linked(true);
        Active(Prescribed(withItems: true, id: "a"));
        _plans.Setup(p => p.GetByIdAsync("b", It.IsAny<CancellationToken>())).ReturnsAsync(Prescribed(withItems: true, id: "b"));

        Assert.True((await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, "b", default)).IsSuccess);
    }

    [Fact]
    public async Task Plan_ActivatingSelfMadePlanOverFullPrescribed_IsLocked()
    {
        Linked(true);
        Active(Prescribed(withItems: true, id: "a"));
        _plans.Setup(p => p.GetByIdAsync("own", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MealPlanDocument { Id = "own", UserId = Client, Name = "own" });

        var result = await Guard().EnsureMealPlanStructureEditableAsync(Client, Client, "own", default);

        Assert.Equal("nutrition.mealPlan.lockedByNutritionist", result.Error!.Code);
    }
}

public class MealPlanMacroTargetsTests
{
    private static CreateMealPlanCommand Cmd(MealPlanItemInputDto[] items, MealPlanMealTargetInputDto[]? meals) =>
        new("Plan", items, Meals: meals);

    private readonly CreateMealPlanCommandValidator _validator = new();

    [Fact]
    public void Validator_AcceptsMacrosOnlyPlanWithoutItems()
    {
        var cmd = Cmd([], [new MealPlanMealTargetInputDto("lunch", 600, 40, 70, 20)]);
        Assert.True(_validator.Validate(cmd).IsValid);
    }

    [Fact]
    public void Validator_RejectsEmptyPlan_TargetWithoutMacros_AndDuplicateSlots()
    {
        Assert.False(_validator.Validate(Cmd([], null)).IsValid);
        Assert.False(_validator.Validate(Cmd([], [new MealPlanMealTargetInputDto("lunch", null, null, null, null)])).IsValid);
        Assert.False(_validator.Validate(Cmd([], [
            new MealPlanMealTargetInputDto("lunch", 500, null, null, null),
            new MealPlanMealTargetInputDto("LUNCH", 400, null, null, null)])).IsValid);
        Assert.False(_validator.Validate(Cmd([], [new MealPlanMealTargetInputDto("brunch", 500, null, null, null)])).IsValid);
        Assert.False(_validator.Validate(Cmd([], [new MealPlanMealTargetInputDto("lunch", -1, null, null, null)])).IsValid);
    }

    [Fact]
    public async Task Handler_ExposesPerMealTargetsAndMacroOnlyFlags()
    {
        var plans = new Mock<IMealPlanRepository>();
        var foods = new Mock<IFoodRepository>();
        foods.Setup(f => f.GetByIdAsync("f", It.IsAny<CancellationToken>())).ReturnsAsync(new FoodDocument());
        var access = Mock.Of<INutritionAccessPolicy>(a => a.CanManageNutritionForAsync(1, 1, It.IsAny<CancellationToken>()) == Task.FromResult(true));
        var guard = Mock.Of<INutritionLockGuard>(g => g.EnsureMealPlanStructureEditableAsync(1, 1, null, It.IsAny<CancellationToken>()) == Task.FromResult(Result.Success()));
        var handler = new CreateMealPlanHandler(plans.Object, foods.Object, access, guard, _validator);

        var mixed = await handler.HandleAsync(new CreateMealPlanCommand(
            "Mixed",
            [new MealPlanItemInputDto("dinner", "f", 100)],
            Meals:
            [
                new MealPlanMealTargetInputDto("lunch", 600, 40, null, null),
                new MealPlanMealTargetInputDto("dinner", 700, null, null, null)
            ]), 1, default);

        Assert.True(mixed.IsSuccess);
        Assert.False(mixed.Value!.IsMacroOnly);
        Assert.True(mixed.Value.Meals!.Single(m => m.MealSlot == "lunch").IsMacroOnly);
        Assert.Equal(600, mixed.Value.Meals!.Single(m => m.MealSlot == "lunch").TargetCalories);
        Assert.False(mixed.Value.Meals!.Single(m => m.MealSlot == "dinner").IsMacroOnly);

        var macros = await handler.HandleAsync(new CreateMealPlanCommand(
            "Macros", [], Meals: [new MealPlanMealTargetInputDto("lunch", 600, 40, 70, 20)]), 1, default);

        Assert.True(macros.IsSuccess);
        Assert.True(macros.Value!.IsMacroOnly);
        plans.Verify(p => p.CreateAsync(It.Is<MealPlanDocument>(d => d.Meals.Count > 0), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handler_WhenGuardLocks_ReturnsForbiddenAndPersistsNothing()
    {
        var plans = new Mock<IMealPlanRepository>();
        var access = Mock.Of<INutritionAccessPolicy>(a => a.CanManageNutritionForAsync(1, 1, It.IsAny<CancellationToken>()) == Task.FromResult(true));
        var guard = Mock.Of<INutritionLockGuard>(g => g.EnsureMealPlanStructureEditableAsync(1, 1, null, It.IsAny<CancellationToken>())
            == Task.FromResult(Result.Failure(ShapeUp.Features.Nutrition.Shared.Errors.NutritionErrors.MealPlanLockedByNutritionist())));
        var handler = new CreateMealPlanHandler(plans.Object, Mock.Of<IFoodRepository>(), access, guard, _validator);

        var result = await handler.HandleAsync(new CreateMealPlanCommand(
            "x", [], Meals: [new MealPlanMealTargetInputDto("lunch", 600, null, null, null)]), 1, default);

        Assert.Equal("nutrition.mealPlan.lockedByNutritionist", result.Error!.Code);
        plans.Verify(p => p.CreateAsync(It.IsAny<MealPlanDocument>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
