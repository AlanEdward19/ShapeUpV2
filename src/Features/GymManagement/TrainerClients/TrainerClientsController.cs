namespace ShapeUp.Features.GymManagement.TrainerClients;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using AcceptTrainerClientInvite;
using AddTrainerClient;
using DeactivateTrainerClientPlan;
using GenerateTrainerClientInvite;
using GetTrainerClients;
using TransferTrainerClient;
using UnassignTrainerClient;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/trainers/{trainerId:int}/clients")]
public class TrainerClientsController : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = "capability:gym.trainer_clients.read")]
    public async Task<IActionResult> GetAll(int trainerId, [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetTrainerClientsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrainerClientsQuery(trainerId, cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [Authorize(Policy = "capability:gym.trainer_clients.create")]
    public async Task<IActionResult> Add(int trainerId, [FromBody] AddTrainerClientCommand command,
        [FromServices] AddTrainerClientHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, trainerId, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { trainerId }, success));
    }

    [HttpPost("invites/{clientEmail}")]
    [Authorize(Policy = "capability:gym.trainer_clients.create")]
    public async Task<IActionResult> GenerateInvite(
        int trainerId,
        [FromRoute] string clientEmail,
        [FromBody] GenerateTrainerClientInviteCommand command,
        [FromServices] GenerateTrainerClientInviteHandler handler,
        CancellationToken cancellationToken)
    {
        var userContext = HttpContext.GetUserContext();
        var trainerName = !string.IsNullOrWhiteSpace(userContext?.DisplayName)
            ? userContext.DisplayName!
            : userContext?.Email ?? $"Trainer {trainerId}";
        
        command.SetClientEmail(clientEmail);
        command.SetTrainerName(trainerName);
        
        var result = await handler.HandleAsync(command, trainerId, cancellationToken);
        return this.ToActionResult(result, success => Created($"/api/gym-management/trainer-client-invites/{success.InviteId}", success));
    }

    [HttpPost("/api/gym-management/trainer-client-invites/accept")]
    public async Task<IActionResult> AcceptInvite(
        [FromBody] AcceptTrainerClientInviteCommand command,
        [FromServices] AcceptTrainerClientInviteHandler handler,
        CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        var result = await handler.HandleAsync(command, currentUserId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPut("{clientId:int}/transfer")]
    [Authorize(Policy = "capability:gym.trainer_clients.transfer")]
    public async Task<IActionResult> Transfer(int trainerId, int clientId, [FromBody] TransferTrainerClientCommand command,
        [FromServices] TransferTrainerClientHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { ClientId = clientId }, trainerId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{clientId:int}")]
    [Authorize(Policy = "capability:gym.trainer_clients.unassign")]
    public async Task<IActionResult> Unassign(
        int trainerId,
        int clientId,
        [FromServices] UnassignTrainerClientHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UnassignTrainerClientCommand(clientId), trainerId, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPatch("{clientId:int}/plan/status")]
    [Authorize(Policy = "capability:gym.trainer_clients.deactivate_plan")]
    public async Task<IActionResult> SetPlanStatus(
        int trainerId,
        int clientId,
        [FromBody] DeactivateTrainerClientPlanCommand command,
        [FromServices] DeactivateTrainerClientPlanHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { ClientId = clientId }, trainerId, cancellationToken);
        return this.ToActionResult(result);
    }
}

