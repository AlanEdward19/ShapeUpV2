using Moq;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationDay;
using ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Nutrition.Clients;

public class NutritionClientAccessTests
{
    private readonly Mock<INutritionAccessPolicy> _policy = new();
    private readonly Mock<IHydrationRepository> _hydration = new();

    [Fact]
    public async Task RunAsync_Allowed_RunsHandlerWithTargetUserId()
    {
        _policy.Setup(p => p.CanManageNutritionForAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var day = new DateOnly(2026, 10, 3);
        _hydration.Setup(h => h.GetDayAsync(2, day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new HydrationDayDocument { UserId = 2, TotalMl = 1500 });
        var handler = new GetHydrationDayHandler(_hydration.Object);

        var result = await new NutritionClientAccess(_policy.Object)
            .RunAsync(1, 2, userId => handler.HandleAsync(day, userId, default), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1500, result.Value!.TotalMl);
        _hydration.Verify(h => h.GetDayAsync(1, It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_Denied_Returns403AndNeverRunsHandler()
    {
        _policy.Setup(p => p.CanManageNutritionForAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var handler = new GetHydrationDayHandler(_hydration.Object);

        var result = await new NutritionClientAccess(_policy.Object)
            .RunAsync(1, 2, userId => handler.HandleAsync(new DateOnly(2026, 10, 3), userId, default), default);

        Assert.True(result.IsFailure);
        Assert.Equal(403, result.Error!.StatusCode);
        _hydration.Verify(h => h.GetDayAsync(It.IsAny<int>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ActivateMealPlan_ForTargetWithoutAccess_ReturnsForbidden()
    {
        _policy.Setup(p => p.CanManageNutritionForAsync(1, 2, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var plans = new Mock<IMealPlanRepository>();
        var handler = new ActivateMealPlanHandler(plans.Object, Mock.Of<IFoodRepository>(), null!, null!, _policy.Object, new ActivateMealPlanCommandValidator());

        var result = await handler.HandleAsync(
            new ActivateMealPlanCommand("507f1f77bcf86cd799439011", new DateOnly(2026, 10, 3), TargetUserId: 2), 1, default);

        Assert.Equal(403, result.Error!.StatusCode);
        plans.Verify(p => p.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
