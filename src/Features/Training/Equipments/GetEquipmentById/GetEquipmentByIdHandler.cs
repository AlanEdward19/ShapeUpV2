using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Equipments.GetEquipmentById;

public class GetEquipmentByIdHandler(IEquipmentRepository equipmentRepository)
{
    public async Task<Result<EquipmentResponse>> HandleAsync(GetEquipmentByIdQuery query, CancellationToken cancellationToken)
    {
        var equipment = await equipmentRepository.GetByIdAsync(query.EquipmentId, cancellationToken);
        if (equipment is null)
            return Result<EquipmentResponse>.Failure(TrainingErrors.EquipmentNotFound(query.EquipmentId));

        return Result<EquipmentResponse>.Success(new EquipmentResponse(equipment.Id, equipment.Name, equipment.NamePt, equipment.Description));
    }
}
