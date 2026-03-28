using FluentValidation;

namespace ShapeUp.Features.Training.Equipments.UpdateEquipment;

public class UpdateEquipmentCommandValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentCommandValidator()
    {
        RuleFor(x => x.EquipmentId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NamePt).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(600);
    }
}