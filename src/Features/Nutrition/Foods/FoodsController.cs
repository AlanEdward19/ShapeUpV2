using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Nutrition.Foods.CreateFood;
using ShapeUp.Features.Nutrition.Foods.GetFoodByBarcode;
using ShapeUp.Features.Nutrition.Foods.SearchFoods;
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
        var result = await handler.HandleAsync(new SearchFoodsQuery(query, cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<IActionResult> GetByBarcode(
        string barcode,
        [FromServices] GetFoodByBarcodeHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetFoodByBarcodeQuery(barcode), cancellationToken);
        return this.ToActionResult(result);
    }
}
