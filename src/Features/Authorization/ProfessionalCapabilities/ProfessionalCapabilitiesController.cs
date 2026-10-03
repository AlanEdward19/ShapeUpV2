using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.ProfessionalCapabilities;

[ApiController]
[Route("api/professional-capabilities")]
public class ProfessionalCapabilitiesController : ControllerBase
{
    // Self-service, like GET user-roles/me: authentication alone is the gate.
    [HttpGet("me")]
    public async Task<IActionResult> GetMine(
        [FromServices] GetProfessionalCapabilitiesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
