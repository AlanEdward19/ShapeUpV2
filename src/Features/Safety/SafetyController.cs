namespace ShapeUp.Features.Safety;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

/// <summary>Report and block, required for apps with user-generated content (App Store 1.2).</summary>
[ApiController]
[Route("api/safety")]
public class SafetyController : ControllerBase
{
    [HttpPost("reports")]
    public async Task<IActionResult> Report(
        [FromBody] CreateReportCommand command, [FromServices] ReportsHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.CreateAsync(command, HttpContext.GetUserId(), cancellationToken),
            success => Created("api/safety/reports", success));

    [HttpGet("reports")]
    [Authorize(Policy = "capability:platform.safety.review")]
    public async Task<IActionResult> ListReports(
        [FromQuery] string? status, [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] ReportsHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.ListAsync(status, cursor, pageSize, cancellationToken));

    [HttpPost("reports/{id:int}/resolve")]
    [Authorize(Policy = "capability:platform.safety.review")]
    public async Task<IActionResult> Resolve(
        int id, [FromBody] ResolveReportCommand command, [FromServices] ReportsHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.ResolveAsync(id, command, HttpContext.GetUserId(), cancellationToken));

    [HttpGet("blocks")]
    public async Task<IActionResult> ListBlocks([FromServices] BlocksHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.ListAsync(HttpContext.GetUserId(), cancellationToken));

    [HttpPost("blocks/{userId:int}")]
    public async Task<IActionResult> Block(int userId, [FromServices] BlocksHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.BlockAsync(HttpContext.GetUserId(), userId, cancellationToken));

    [HttpDelete("blocks/{userId:int}")]
    public async Task<IActionResult> Unblock(int userId, [FromServices] BlocksHandler handler, CancellationToken cancellationToken) =>
        this.ToActionResult(await handler.UnblockAsync(HttpContext.GetUserId(), userId, cancellationToken));
}
