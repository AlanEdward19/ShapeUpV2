using FluentValidation;

namespace ShapeUp.Features.Training.Equipments.CreateEquipment;

public class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.NamePt).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(600);
    }
}