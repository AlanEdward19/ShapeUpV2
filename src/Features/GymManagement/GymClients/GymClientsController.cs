namespace ShapeUp.Features.GymManagement.GymClients;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using AssignClientTrainer;
using EnrollGymClient;
using GetGymClients;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/gyms/{gymId:int}/clients")]
public class GymClientsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "capability:gym.clients.read")]
    public async Task<IActionResult> GetAll(int gymId, [FromQuery] string? cursor, [FromQuery] int? pageSize, [FromQuery] int? trainerId,
        [FromServices] GetGymClientsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetGymClientsQuery(gymId, cursor, pageSize, trainerId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = "capability:gym.clients.create")]
    public async Task<IActionResult> Enroll(int gymId, [FromBody] EnrollGymClientCommand command,
        [FromServices] EnrollGymClientHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { GymId = gymId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { gymId }, success));
    }

    [HttpPut("{clientId:int}/trainer")]
    [Authorize(Policy = "capability:gym.clients.assign_trainer")]
    public async Task<IActionResult> AssignTrainer(int gymId, int clientId, [FromBody] AssignClientTrainerCommand command,
        [FromServices] AssignClientTrainerHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { GymId = gymId, ClientId = clientId }, HttpContext.GetUserId(), cancellationToken);
        return this.ToActionResult(result);
    }
}
