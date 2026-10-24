using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;
using ShapeUp.Features.Nutrition.Profile.GetNutritionProfile;
using ShapeUp.Features.Nutrition.Profile.SetDietaryRestrictions;
using ShapeUp.Features.Nutrition.Profile.SetManualGoal;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Profile;

[ApiController]
[Route("api/nutrition/profile")]
public class NutritionProfileController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromServices] GetNutritionProfileHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetNutritionProfileQuery(), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("onboarding")]
    public async Task<IActionResult> CompleteOnboarding(
        [FromBody] CompleteOnboardingCommand command,
        [FromServices] CompleteOnboardingHandler handler,
        [FromServices] INutritionLockGuard lockGuard,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var lockResult = await lockGuard.EnsureGoalEditableAsync(userId, userId, cancellationToken);
        if (!lockResult.IsSuccess)
            return this.ToActionResult(lockResult);

        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("goal")]
    public async Task<IActionResult> SetManualGoal(
        [FromBody] SetManualGoalCommand command,
        [FromServices] SetManualGoalHandler handler,
        [FromServices] INutritionLockGuard lockGuard,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();
        var lockResult = await lockGuard.EnsureGoalEditableAsync(userId, userId, cancellationToken);
        if (!lockResult.IsSuccess)
            return this.ToActionResult(lockResult);

        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("restrictions")]
    public async Task<IActionResult> SetRestrictions(
        [FromBody] SetDietaryRestrictionsCommand command,
        [FromServices] SetDietaryRestrictionsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
