namespace ShapeUp.Features.AuditLogs.GetAuditLogs;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/audit-logs")]
public class GetAuditLogsController(GetAuditLogsHandler handler) : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "audit:logs:read" }])]
    public async Task<IActionResult> Get(
        [FromQuery] GetAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }
}

