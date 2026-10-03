using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Clients.AcceptInvite;
using ShapeUp.Features.Nutrition.Clients.ClientsAdherence;
using ShapeUp.Features.Nutrition.Clients.ListInvites;
using ShapeUp.Features.Nutrition.Clients.ListNutritionists;
using ShapeUp.Features.Nutrition.Clients.RevokeInvite;
using ShapeUp.Features.Nutrition.Comments.AddComment;
using ShapeUp.Features.Nutrition.Comments.GetComments;
using ShapeUp.Features.Nutrition.Fasting.GetClock;
using ShapeUp.Features.Nutrition.Fasting.GetHistory;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.MealPlans.GetActiveMealPlan;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlanById;
using ShapeUp.Features.Nutrition.MealPlans.GetMealPlans;
using ShapeUp.Features.Nutrition.Measurements.AddMeasurement;
using ShapeUp.Features.Nutrition.Measurements.GetMeasurements;
using ShapeUp.Features.Nutrition.Profile.SetDietaryRestrictions;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
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

    /// <summary>The nutritionist's pending invites (no tokens), paginated by cursor.</summary>
    [HttpGet("clients/invites")]
    public async Task<IActionResult> ListInvites(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] ListNutritionInvitesHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(new ListNutritionInvitesQuery(cursor, pageSize), HttpContext.GetUserId(), cancellationToken));

    /// <summary>The nutritionist revokes a pending invite.</summary>
    [HttpDelete("clients/invites/{inviteId:int}")]
    public async Task<IActionResult> RevokeInvite(
        int inviteId,
        [FromServices] RevokeNutritionInviteHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(inviteId, HttpContext.GetUserId(), cancellationToken));

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

    /// <summary>
    /// Adherence of the nutritionist's clients over the last <c>days</c> (default 7, max 90), paginated by cursor.
    /// "Today" follows each client's saved time zone, UTC otherwise.
    /// </summary>
    [HttpGet("clients/adherence")]
    public async Task<IActionResult> GetClientsAdherence(
        [FromQuery] int? days,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetClientsAdherenceHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(days, cursor, pageSize, HttpContext.GetUserId(), cancellationToken));

    /// <summary>Client side: the nutritionists linked to the logged user.</summary>
    [HttpGet("nutritionists")]
    public async Task<IActionResult> ListNutritionists(
        [FromServices] ListMyNutritionistsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken));

    [HttpPut("users/{targetUserId:int}/restrictions")]
    public async Task<IActionResult> SetRestrictions(
        int targetUserId,
        [FromBody] SetDietaryRestrictionsCommand command,
        [FromServices] NutritionClientAccess access,
        [FromServices] SetDietaryRestrictionsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await access.RunAsync(
            HttpContext.GetUserId(), targetUserId,
            userId => handler.HandleAsync(command, userId, cancellationToken),
            cancellationToken));

    [HttpGet("users/{targetUserId:int}/meal-plans")]
    public async Task<IActionResult> GetMealPlans(
        int targetUserId,
        [FromServices] GetMealPlansHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), targetUserId, cancellationToken));

    [HttpGet("users/{targetUserId:int}/meal-plans/active")]
    public async Task<IActionResult> GetActiveMealPlan(
        int targetUserId,
        [FromServices] GetActiveMealPlanHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), targetUserId, cancellationToken));

    [HttpGet("users/{targetUserId:int}/meal-plans/{mealPlanId}")]
    public async Task<IActionResult> GetMealPlanById(
        int targetUserId,
        string mealPlanId,
        [FromServices] GetMealPlanByIdHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(mealPlanId, HttpContext.GetUserId(), targetUserId, cancellationToken));

    [HttpPost("users/{targetUserId:int}/measurements")]
    public async Task<IActionResult> AddMeasurement(
        int targetUserId,
        [FromBody] AddMeasurementCommand command,
        [FromServices] AddMeasurementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), targetUserId, cancellationToken);
        return this.ToActionResult(result, success => Created($"api/nutrition/users/{targetUserId}/measurements", success));
    }

    [HttpGet("users/{targetUserId:int}/measurements")]
    public async Task<IActionResult> GetMeasurements(
        int targetUserId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetMeasurementsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(
            new GetMeasurementsQuery(from, to, cursor, pageSize), HttpContext.GetUserId(), targetUserId, cancellationToken));

    /// <summary>The nutritionist comments on a day of the client's diary (or on one entry with <c>entryId</c>).</summary>
    [HttpPost("users/{targetUserId:int}/diary/comments")]
    public async Task<IActionResult> AddDiaryComment(
        int targetUserId,
        [FromBody] AddDiaryCommentCommand command,
        [FromServices] AddDiaryCommentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), targetUserId, cancellationToken);
        return this.ToActionResult(result, success => Created($"api/nutrition/users/{targetUserId}/diary/comments", success));
    }

    /// <summary>One day with <c>?date=</c>, or a range with <c>?from=&amp;to=</c>; the client reads the comments on their own diary.</summary>
    [HttpGet("users/{targetUserId:int}/diary/comments")]
    public async Task<IActionResult> GetDiaryComments(
        int targetUserId,
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetDiaryCommentsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(
            new GetDiaryCommentsQuery(date, from, to, cursor, pageSize), HttpContext.GetUserId(), targetUserId, cancellationToken));

    /// <summary>Read-only fasting snapshot (agenda, override, clock) of the client.</summary>
    [HttpGet("users/{targetUserId:int}/fasting")]
    public async Task<IActionResult> GetFasting(
        int targetUserId,
        [FromServices] FastingFeatureGuard guard,
        [FromServices] NutritionClientAccess access,
        [FromServices] GetFastingClockHandler handler,
        CancellationToken cancellationToken)
    {
        var guardResult = await guard.EnsureEnabledAsync(cancellationToken);
        if (!guardResult.IsSuccess)
            return this.ToActionResult(guardResult);

        return this.ToActionResult(await access.RunAsync(
            HttpContext.GetUserId(), targetUserId,
            userId => handler.HandleAsync(new GetFastingClockQuery(), userId, cancellationToken),
            cancellationToken));
    }

    [HttpGet("users/{targetUserId:int}/fasting/history")]
    public async Task<IActionResult> GetFastingHistory(
        int targetUserId,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] FastingFeatureGuard guard,
        [FromServices] NutritionClientAccess access,
        [FromServices] GetFastingHistoryHandler handler,
        CancellationToken cancellationToken)
    {
        var guardResult = await guard.EnsureEnabledAsync(cancellationToken);
        if (!guardResult.IsSuccess)
            return this.ToActionResult(guardResult);

        return this.ToActionResult(await access.RunAsync(
            HttpContext.GetUserId(), targetUserId,
            userId => handler.HandleAsync(new GetFastingHistoryQuery(cursor, pageSize), userId, cancellationToken),
            cancellationToken));
    }
}
