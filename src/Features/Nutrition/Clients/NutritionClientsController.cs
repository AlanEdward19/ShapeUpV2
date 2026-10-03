using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Clients.AcceptInvite;
using ShapeUp.Features.Nutrition.Clients.EndRelationship;
using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.ListClients;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Diary.GetDiaryDay;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationDay;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationRange;
using ShapeUp.Features.Nutrition.Profile.GetNutritionProfile;
using ShapeUp.Features.Nutrition.Profile.SetManualGoal;
using ShapeUp.Features.Nutrition.WeightTracking.GetWeightRegisters;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Clients;

/// <summary>
/// Professional nutrition: the nutritionist's clients and read/prescribe access to a client's data.
/// Every <c>users/{targetUserId}</c> route is gated by <see cref="ShapeUp.Features.Nutrition.Shared.Abstractions.INutritionAccessPolicy"/> (403 otherwise).
/// </summary>
[ApiController]
[Route("api/nutrition")]
public class NutritionClientsController : ControllerBase
{
    [HttpGet("clients")]
    public async Task<IActionResult> ListClients(
        [FromServices] ListNutritionClientsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken));

    /// <summary>The nutritionist generates a single-use invite token for a new client.</summary>
    [HttpPost("clients/invites")]
    public async Task<IActionResult> InviteClient(
        [FromServices] InviteNutritionClientHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken));

    /// <summary>The client accepts the invite, consenting to share nutrition data with the nutritionist.</summary>
    [HttpPost("clients/invites/accept")]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptNutritionInviteCommand command,
        [FromServices] AcceptNutritionInviteHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken));

    /// <summary>The nutritionist ends the link with a client.</summary>
    [HttpDelete("clients/{clientUserId:int}")]
    public async Task<IActionResult> RemoveClient(
        int clientUserId,
        [FromServices] EndNutritionRelationshipHandler handler,
        CancellationToken cancellationToken)
    {
        var actor = HttpContext.GetUserId();
        return this.ToActionResult(await handler.HandleAsync(actor, actor, clientUserId, cancellationToken));
    }

    /// <summary>The client revokes a nutritionist's access.</summary>
    [HttpDelete("nutritionists/{professionalUserId:int}")]
    public async Task<IActionResult> RemoveNutritionist(
        int professionalUserId,
        [FromServices] EndNutritionRelationshipHandler handler,
        CancellationToken cancellationToken)
    {
        var actor = HttpContext.GetUserId();
        return this.ToActionResult(await handler.HandleAsync(actor, professionalUserId, actor, cancellationToken));
    }

    [HttpGet("users/{targetUserId:int}/diary")]
    public async Task<IActionResult> GetDiary(
        int targetUserId,
        [FromQuery] DateOnly date,
        [FromServices] NutritionClientAccess access,
        [FromServices] GetDiaryDayHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await access.RunAsync(
            HttpContext.GetUserId(), targetUserId,
            userId => handler.HandleAsync(new GetDiaryDayQuery(date), userId, cancellationToken),
            cancellationToken));

    [HttpGet("users/{targetUserId:int}/profile")]
    public async Task<IActionResult> GetProfile(
        int targetUserId,
        [FromServices] NutritionClientAccess access,
        [FromServices] GetNutritionProfileHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await access.RunAsync(
            HttpContext.GetUserId(), targetUserId,
            userId => handler.HandleAsync(new GetNutritionProfileQuery(), userId, cancellationToken),
            cancellationToken));

    [HttpGet("users/{targetUserId:int}/weight/registers")]
    public async Task<IActionResult> GetWeightRegisters(
        int targetUserId,
        [FromQuery] DateTime startDateUtc,
        [FromQuery] DateTime endDateUtc,
        [FromServices] NutritionClientAccess access,
        [FromServices] GetWeightRegistersHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await access.RunAsync(
            HttpContext.GetUserId(), targetUserId,
            userId => handler.HandleAsync(new GetWeightRegistersQuery(startDateUtc, endDateUtc), userId, cancellationToken),
            cancellationToken));

    /// <summary>One day with <c>?date=</c>, or the days with a record in <c>?from=&amp;to=</c>.</summary>
    [HttpGet("users/{targetUserId:int}/hydration")]
    public async Task<IActionResult> GetHydration(
        int targetUserId,
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromServices] NutritionClientAccess access,
        [FromServices] GetHydrationDayHandler dayHandler,
        [FromServices] GetHydrationRangeHandler rangeHandler,
        CancellationToken cancellationToken)
    {
        var actor = HttpContext.GetUserId();

        if (date.HasValue)
            return this.ToActionResult(await access.RunAsync(actor, targetUserId,
                userId => dayHandler.HandleAsync(date.Value, userId, cancellationToken), cancellationToken));

        if (from.HasValue && to.HasValue)
            return this.ToActionResult(await access.RunAsync(actor, targetUserId,
                userId => rangeHandler.HandleAsync(new GetHydrationRangeQuery(from.Value, to.Value), userId, cancellationToken), cancellationToken));

        return this.ToActionResult(Result.Failure(CommonErrors.Validation("Provide either 'date' or both 'from' and 'to'.")));
    }

    /// <summary>Prescribes the client's calories, macros and (optionally) water goal.</summary>
    [HttpPut("users/{targetUserId:int}/goal")]
    public async Task<IActionResult> SetGoal(
        int targetUserId,
        [FromBody] SetManualGoalCommand command,
        [FromServices] NutritionClientAccess access,
        [FromServices] SetManualGoalHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await access.RunAsync(
            HttpContext.GetUserId(), targetUserId,
            userId => handler.HandleAsync(command, userId, cancellationToken),
            cancellationToken));
}
