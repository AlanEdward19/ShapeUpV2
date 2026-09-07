using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Features.Training.WeightTracking.GetWeightRegisters;
using ShapeUp.Features.Training.WeightTracking.UpsertDailyWeightRegister;
using ShapeUp.Features.Training.WeightTracking.UpsertTargetWeight;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WeightTracking;

[ApiController]
[Route("api/training/weight")]
public class WeightTrackingController : ControllerBase
{
    [HttpPut("target")]
    public async Task<IActionResult> UpsertTarget(
        [FromBody] UpsertTargetWeightCommand command,
        [FromServices] UpsertTargetWeightHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost("registers")]
    public async Task<IActionResult> UpsertDailyRegister(
        [FromBody] UpsertDailyWeightRegisterCommand command,
        [FromServices] UpsertDailyWeightRegisterHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("registers")]
    public async Task<IActionResult> GetRegisters(
        [FromQuery] DateTime startDateUtc,
        [FromQuery] DateTime endDateUtc,
        [FromServices] GetWeightRegistersHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetWeightRegistersQuery(startDateUtc, endDateUtc);
        var result = await handler.HandleAsync(query, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
