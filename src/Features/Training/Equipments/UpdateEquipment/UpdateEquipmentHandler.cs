using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Equipments.UpdateEquipment;

public class UpdateEquipmentHandler(IEquipmentRepository equipmentRepository, IValidator<UpdateEquipmentCommand> validator)
{
    public async Task<Result<EquipmentResponse>> HandleAsync(UpdateEquipmentCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<EquipmentResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var equipment = await equipmentRepository.GetByIdAsync(command.EquipmentId, cancellationToken);
        if (equipment is null)
            return Result<EquipmentResponse>.Failure(TrainingErrors.EquipmentNotFound(command.EquipmentId));

        equipment.Name = command.Name.Trim();
        equipment.NamePt = command.NamePt.Trim();
        equipment.Description = command.Description;

        await equipmentRepository.UpdateAsync(equipment, cancellationToken);
        return Result<EquipmentResponse>.Success(new EquipmentResponse(equipment.Id, equipment.Name, equipment.NamePt, equipment.Description));
    }
}