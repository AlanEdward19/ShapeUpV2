using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Fasting.CancelOverride;
using ShapeUp.Features.Nutrition.Fasting.EndOverrideEarly;
using ShapeUp.Features.Nutrition.Fasting.GetClock;
using ShapeUp.Features.Nutrition.Fasting.GetHistory;
using ShapeUp.Features.Nutrition.Fasting.PutAgenda;
using ShapeUp.Features.Nutrition.Fasting.SetRecommendation;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Fasting.StartOverride;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Fasting;

[ApiController]
[Route("api/nutrition/fasting")]
public sealed class FastingController : ControllerBase
{
    private async Task<IActionResult?> EnsureFastingEnabledAsync(
        FastingFeatureGuard guard,
        CancellationToken cancellationToken)
    {
        var guardResult = await guard.EnsureEnabledAsync(cancellationToken);
        if (!guardResult.IsSuccess)
            return this.ToActionResult(guardResult);

        return null;
    }

    [HttpGet]
    public async Task<IActionResult> GetClock(
        [FromServices] FastingFeatureGuard guard,
        [FromServices] GetFastingClockHandler handler,
        CancellationToken cancellationToken)
    {
        var blocked = await EnsureFastingEnabledAsync(guard, cancellationToken);
        if (blocked is not null)
            return blocked;

        var result = await handler.HandleAsync(new GetFastingClockQuery(), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("agenda")]
    public async Task<IActionResult> PutAgenda(
        [FromBody] PutFastingAgendaCommand command,
        [FromServices] FastingFeatureGuard guard,
        [FromServices] PutFastingAgendaHandler handler,
        CancellationToken cancellationToken)
    {
        var blocked = await EnsureFastingEnabledAsync(guard, cancellationToken);
        if (blocked is not null)
            return blocked;

        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("override/start")]
    public async Task<IActionResult> StartOverride(
        [FromServices] FastingFeatureGuard guard,
        [FromServices] StartFastingOverrideHandler handler,
        CancellationToken cancellationToken)
    {
        var blocked = await EnsureFastingEnabledAsync(guard, cancellationToken);
        if (blocked is not null)
            return blocked;

        var result = await handler.HandleAsync(new StartFastingOverrideCommand(), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, dto => Created(string.Empty, dto));
    }

    [HttpPost("override/end-early")]
    public async Task<IActionResult> EndOverrideEarly(
        [FromServices] FastingFeatureGuard guard,
        [FromServices] EndFastingOverrideEarlyHandler handler,
        CancellationToken cancellationToken)
    {
        var blocked = await EnsureFastingEnabledAsync(guard, cancellationToken);
        if (blocked is not null)
            return blocked;

        var result = await handler.HandleAsync(new EndFastingOverrideEarlyCommand(), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("override/cancel")]
    public async Task<IActionResult> CancelOverride(
        [FromServices] FastingFeatureGuard guard,
        [FromServices] CancelFastingOverrideHandler handler,
        CancellationToken cancellationToken)
    {
        var blocked = await EnsureFastingEnabledAsync(guard, cancellationToken);
        if (blocked is not null)
            return blocked;

        var result = await handler.HandleAsync(new CancelFastingOverrideCommand(), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("recommendation/{clientUserId:int}")]
    public async Task<IActionResult> SetRecommendation(
        int clientUserId,
        [FromBody] SetFastingRecommendationCommand command,
        [FromServices] FastingFeatureGuard guard,
        [FromServices] SetFastingRecommendationHandler handler,
        CancellationToken cancellationToken)
    {
        var blocked = await EnsureFastingEnabledAsync(guard, cancellationToken);
        if (blocked is not null)
            return blocked;

        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), clientUserId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetHistory(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] FastingFeatureGuard guard,
        [FromServices] GetFastingHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var blocked = await EnsureFastingEnabledAsync(guard, cancellationToken);
        if (blocked is not null)
            return blocked;

        var result = await handler.HandleAsync(
            new GetFastingHistoryQuery(cursor, pageSize),
            HttpContext.GetUserId(),
            cancellationToken);
        return this.ToActionResult(result);
    }
}
