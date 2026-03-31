namespace ShapeUp.Features.GymManagement.TrainerClients;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Features.Authorization.Shared.Extensions;
using AcceptTrainerClientInvite;
using AddTrainerClient;
using GenerateTrainerClientInvite;
using GetTrainerClients;
using TransferTrainerClient;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/trainers/{trainerId:int}/clients")]
public class TrainerClientsController : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_clients:read" }])]
    public async Task<IActionResult> GetAll(int trainerId, [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetTrainerClientsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrainerClientsQuery(trainerId, cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_clients:create" }])]
    public async Task<IActionResult> Add(int trainerId, [FromBody] AddTrainerClientCommand command,
        [FromServices] AddTrainerClientHandler handler, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        if (trainerId != currentUserId)
            return this.ToActionResult(Result<AddTrainerClientResponse>.Failure(CommonErrors.Forbidden("You can only add clients to yourself.")));
        var result = await handler.HandleAsync(command, trainerId, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { trainerId }, success));
    }

    [HttpPost("invites/{clientEmail}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_clients:create" }])]
    public async Task<IActionResult> GenerateInvite(
        int trainerId,
        [FromRoute] string clientEmail,
        [FromBody] GenerateTrainerClientInviteCommand command,
        [FromServices] GenerateTrainerClientInviteHandler handler,
        CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        if (trainerId != currentUserId)
            return this.ToActionResult(Result<GenerateTrainerClientInviteResponse>.Failure(CommonErrors.Forbidden("You can only invite clients for yourself.")));

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
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "gym:trainer_clients:transfer" }])]
    public async Task<IActionResult> Transfer(int trainerId, int clientId, [FromBody] TransferTrainerClientCommand command,
        [FromServices] TransferTrainerClientHandler handler, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        if (trainerId != currentUserId)
            return this.ToActionResult(Result<TransferTrainerClientResponse>.Failure(CommonErrors.Forbidden("You can only transfer clients from yourself.")));
        var result = await handler.HandleAsync(command with { ClientId = clientId }, trainerId, cancellationToken);
        return this.ToActionResult(result);
    }
}

