using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationDay;
using ShapeUp.Features.Nutrition.Hydration.GetHydrationRange;
using ShapeUp.Features.Nutrition.Hydration.SetHydrationDay;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Hydration;

[ApiController]
[Route("api/nutrition/hydration")]
public class HydrationController : ControllerBase
{
    [HttpPut("{date}")]
    public async Task<IActionResult> SetDay(
        DateOnly date,
        [FromBody] SetHydrationDayBody body,
        [FromServices] SetHydrationDayHandler handler,
        CancellationToken cancellationToken)
    {
        // An absent total must not be read as 0, which would wipe the day.
        if (body.TotalMl is null)
            return this.ToActionResult(Result.Failure(CommonErrors.Validation("TotalMl is required.")));

        var result = await handler.HandleAsync(new SetHydrationDayCommand(date, body.TotalMl.Value, body.UpdatedAtUtc), HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    /// <summary>One day with <c>?date=</c>, or the days with a record in <c>?from=&amp;to=</c>.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromServices] GetHydrationDayHandler dayHandler,
        [FromServices] GetHydrationRangeHandler rangeHandler,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserId();

        if (date.HasValue)
            return this.ToActionResult(await dayHandler.HandleAsync(date.Value, userId, cancellationToken));

        if (from.HasValue && to.HasValue)
            return this.ToActionResult(await rangeHandler.HandleAsync(new GetHydrationRangeQuery(from.Value, to.Value), userId, cancellationToken));

        return this.ToActionResult(Result.Failure(CommonErrors.Validation("Provide either 'date' or both 'from' and 'to'.")));
    }

    public record SetHydrationDayBody(int? TotalMl, DateTime? UpdatedAtUtc = null);
}
