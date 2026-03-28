using ShapeUp.Features.Training.Equipments.CreateEquipment;
using ShapeUp.Features.Training.Equipments.DeleteEquipment;
using ShapeUp.Features.Training.Equipments.GetEquipmentById;
using ShapeUp.Features.Training.Equipments.GetEquipments;
using ShapeUp.Features.Training.Equipments.UpdateEquipment;

namespace ShapeUp.Features.Training.Equipments;

using Microsoft.AspNetCore.Mvc;
using ShapeUp.Features.Authorization.Infrastructure.Authorization;
using ShapeUp.Shared.Results;

[ApiController]
[Route("api/training/equipments")]
public class EquipmentsController : ControllerBase
{
    [HttpGet]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:equipments:read" }])]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor,
        [FromQuery] int? pageSize,
        [FromServices] GetEquipmentsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEquipmentsQuery(cursor, pageSize), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpGet("{equipmentId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:equipments:read" }])]
    public async Task<IActionResult> GetById(
        int equipmentId,
        [FromServices] GetEquipmentByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetEquipmentByIdQuery(equipmentId), cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpPost]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:equipments:create" }])]
    public async Task<IActionResult> Create(
        [FromBody] CreateEquipmentCommand command,
        [FromServices] CreateEquipmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(result, success => CreatedAtAction(nameof(GetById), new { equipmentId = success.Id }, success));
    }

    [HttpPut("{equipmentId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:equipments:update" }])]
    public async Task<IActionResult> Update(
        int equipmentId,
        [FromBody] UpdateEquipmentCommand command,
        [FromServices] UpdateEquipmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command with { EquipmentId = equipmentId }, cancellationToken);
        return this.ToActionResult(result);
    }

    [HttpDelete("{equipmentId:int}")]
    [TypeFilter(typeof(RequireScopesAttribute), Arguments = [new[] { "training:equipments:delete" }])]
    public async Task<IActionResult> Delete(
        int equipmentId,
        [FromServices] DeleteEquipmentHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteEquipmentCommand(equipmentId), cancellationToken);
        return this.ToActionResult(result);
    }
}

