using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Meals;

[ApiController]
[Route("api/nutrition/meals")]
public class MealsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromServices] ListUserMealsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken));

    [HttpPut("{id}")]
    public async Task<IActionResult> Save(
        string id,
        [FromBody] SaveUserMealBody body,
        [FromServices] SaveUserMealHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(new SaveUserMealCommand(id, body), HttpContext.GetUserId(), cancellationToken));

    /// <summary>Several changes in one request (create/change, delete and reorder), applied atomically.</summary>
    [HttpPost("batch")]
    public async Task<IActionResult> Batch(
        [FromBody] BatchUserMealsBody body,
        [FromServices] BatchUserMealsHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(body, HttpContext.GetUserId(), cancellationToken));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(
        string id,
        [FromServices] DeleteUserMealHandler handler,
        CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.HandleAsync(id, HttpContext.GetUserId(), cancellationToken));
}
