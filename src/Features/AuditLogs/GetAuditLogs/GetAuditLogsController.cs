namespace ShapeUp.Features.AuditLogs.GetAuditLogs;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/audit-logs")]
public class GetAuditLogsController(GetAuditLogsHandler handler) : ControllerBase
{
    // Reading the platform's full HTTP audit trail (any user's requests) is a platform-admin action.
    [HttpGet]
    [Authorize(Policy = "capability:platform.audit_logs.read")]
    public async Task<IActionResult> Get(
        [FromQuery] GetAuditLogsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return this.ToActionResult(result);
    }
}

