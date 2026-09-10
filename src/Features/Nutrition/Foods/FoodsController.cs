using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Foods.CreateFood;
using ShapeUp.Features.Nutrition.Foods.CreateFoodOverride;
using ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;
using ShapeUp.Features.Nutrition.Foods.SearchFoods;
using ShapeUp.Features.Nutrition.Foods.SetActiveFoodVersion;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Foods;

[ApiController]
[Route("api/nutrition/foods")]
public class FoodsController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateFoodCommand command,
        [FromServices] CreateFoodHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetByBarcode), new { barcode = success.Barcode ?? success.Id }, success));
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? query,
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] SearchFoodsHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserContext()?.UserId;
        var result = await handler.HandleAsync(new SearchFoodsQuery(query, cursor, pageSize), userId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(
        string barcode,
        [FromServices] GetFoodByBarcodeHandler handler,
        CancellationToken cancellationToken)
    {
        var userId = HttpContext.GetUserContext()?.UserId;
        var result = await handler.HandleAsync(new GetFoodByBarcodeQuery(barcode), userId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("{foodId}/override")]
    public async Task<IActionResult> CreateOverride(
        string foodId,
        [FromBody] CreateFoodOverrideBody body,
        [FromServices] CreateFoodOverrideHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateFoodOverrideCommand(foodId, body.MacrosPer100, body.MicrosPer100);
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{foodId}/active-version")]
    public async Task<IActionResult> SetActiveVersion(
        string foodId,
        [FromBody] SetActiveFoodVersionCommand command,
        [FromServices] SetActiveFoodVersionHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(foodId, command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    public record CreateFoodOverrideBody(
        Shared.ViewModels.MacroInputDto MacrosPer100,
        Shared.ViewModels.MicroInputDto? MicrosPer100);
}
