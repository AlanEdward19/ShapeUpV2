namespace ShapeUp.Features.GymManagement.TrainerClients;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Shared.Extensions;
using AddTrainerClient;
using GetTrainerClients;
using TransferTrainerClient;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/gym-management/trainers/{trainerId:int}/clients")]
public class TrainerClientsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(int trainerId, [FromQuery] string? cursor, [FromQuery] int? pageSize,
        [FromServices] GetTrainerClientsHandler handler, CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetTrainerClientsQuery(trainerId, cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add(int trainerId, [FromBody] AddTrainerClientCommand command,
        [FromServices] AddTrainerClientHandler handler, CancellationToken cancellationToken)
    {
        var currentUserId = HttpContext.GetUserId();
        if (trainerId != currentUserId)
            return this.ToActionResult(Result<AddTrainerClientResponse>.Failure(CommonErrors.Forbidden("You can only add clients to yourself.")));
        var result = await handler.HandleAsync(command, trainerId, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetAll), new { trainerId }, success));
    }

    [HttpPut("{clientId:int}/transfer")]
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

