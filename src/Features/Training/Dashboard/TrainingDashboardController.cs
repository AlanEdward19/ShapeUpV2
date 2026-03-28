using ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;

namespace ShapeUp.Features.Training.Dashboard;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/training/dashboard")]
public class TrainingDashboardController : ControllerBase
{
    [HttpGet("me")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:dashboard:read" }])]
    public async Task<IActionResult> GetMyDashboard(
        [FromQuery] int sessionsTargetPerWeek,
        [FromServices] GetTrainingDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrainingDashboardQuery(HttpContext.GetUserId(), sessionsTargetPerWeek), cancellationToken);
        return this.ToActionResult(result);
    }
}

