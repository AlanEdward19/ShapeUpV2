using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.MealPlans.ActivateMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.GetActiveMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlanById;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlans;
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
        [FromQuery] int? targetUserId,
        [FromServices] ActivateMealPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ActivateMealPlanCommand(mealPlanId, date, targetUserId), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>The logged user's meal plans, newest first (a professional reads a client's under <c>users/{id}/meal-plans</c>).</summary>
    [HttpGet]
    public async Task<IActionResult> GetMine(
        [FromServices] GetMealPlansHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        return this.ToActionResult(await handler.HandleAsync(userId, userId, cancellationToken));
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(
        [FromServices] GetActiveMealPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        return this.ToActionResult(await handler.HandleAsync(userId, userId, cancellationToken));
    }

    [HttpGet("{mealPlanId}")]
    public async Task<IActionResult> GetById(
        string mealPlanId,
        [FromServices] GetMealPlanByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        return this.ToActionResult(await handler.HandleAsync(mealPlanId, userId, userId, cancellationToken));
    }
}
