using ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;
using ShapeUp.Features.Training.Dashboard.GetWeeklyReading;

namespace ShapeUp.Features.Training.Dashboard;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/training/dashboard")]
public class TrainingDashboardController : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyDashboard(
        [FromQuery] int sessionsTargetPerWeek,
        [FromServices] GetTrainingDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrainingDashboardQuery(HttpContext.GetUserId(), sessionsTargetPerWeek), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("me/weekly-reading")]
    public async Task<IActionResult> GetMyWeeklyReading(
        [FromServices] GetWeeklyReadingHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetWeeklyReadingQuery(HttpContext.GetUserId()), cancellationToken);
        return this.ToActionResult(result);
    }
}
