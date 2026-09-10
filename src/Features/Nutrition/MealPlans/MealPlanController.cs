using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans;

[ApiController]
[Route("api/nutrition/meal-plans")]
public class MealPlanController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateMealPlanCommand command,
        [FromServices] CreateMealPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(Create), new { id = success.Id }, success));
    }

    [HttpPost("{mealPlanId}/activate")]
    public async Task<IActionResult> Activate(
        string mealPlanId,
        [FromQuery] DateOnly date,
        [FromServices] ActivateMealPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ActivateMealPlanCommand(mealPlanId, date), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
