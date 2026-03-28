using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Equipments.DeleteEquipment;

public class DeleteEquipmentHandler(IEquipmentRepository equipmentRepository)
{
    public async Task<Result> HandleAsync(DeleteEquipmentCommand command, CancellationToken cancellationToken)
    {
        var equipment = await equipmentRepository.GetByIdAsync(command.EquipmentId, cancellationToken);
        if (equipment is null)
            return Result.Failure(TrainingErrors.EquipmentNotFound(command.EquipmentId));

        await equipmentRepository.DeleteAsync(equipment, cancellationToken);
        return Result.Success();
    }
}