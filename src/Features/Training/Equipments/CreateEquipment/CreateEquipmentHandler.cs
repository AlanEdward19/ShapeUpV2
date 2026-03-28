using FluentValidation;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Equipments.CreateEquipment;

public class CreateEquipmentHandler(IEquipmentRepository equipmentRepository, IValidator<CreateEquipmentCommand> validator)
{
    public async Task<Result<EquipmentResponse>> HandleAsync(CreateEquipmentCommand command, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<EquipmentResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var equipment = new Equipment
        {
            Name = command.Name.Trim(),
            NamePt = command.NamePt.Trim(),
            Description = command.Description
        };

        await equipmentRepository.AddAsync(equipment, cancellationToken);
        return Result<EquipmentResponse>.Success(new EquipmentResponse(equipment.Id, equipment.Name, equipment.NamePt, equipment.Description));
    }
}