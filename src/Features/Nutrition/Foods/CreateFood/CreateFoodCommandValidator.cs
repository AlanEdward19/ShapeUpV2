using FluentValidation;
using ShapeUp.Features.Nutrition.Foods.Shared.ViewModels;

namespace ShapeUp.Features.Nutrition.Foods.CreateFood;

public class CreateFoodCommandValidator : AbstractValidator<CreateFoodCommand>
{
    public CreateFoodCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Barcode)
            .MaximumLength(64)
            .When(x => !string.IsNullOrWhiteSpace(x.Barcode));

        RuleFor(x => x.MacrosPer100)
            .NotNull()
            .WithMessage("MacrosPer100 is required.")
            .ChildRules(macros =>
            {
                macros.RuleFor(x => x!.Kcal)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("MacrosPer100.Kcal must be greater than or equal to 0.");
                macros.RuleFor(x => x!.ProteinG)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("MacrosPer100.ProteinG must be greater than or equal to 0.");
                macros.RuleFor(x => x!.CarbG)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("MacrosPer100.CarbG must be greater than or equal to 0.");
                macros.RuleFor(x => x!.FatG)
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("MacrosPer100.FatG must be greater than or equal to 0.");
            });

        RuleFor(x => x.MicrosPer100)
            .ChildRules(micros =>
            {
                micros.RuleFor(x => x!.VitaminAMcg).GreaterThanOrEqualTo(0).When(x => x!.VitaminAMcg.HasValue);
                micros.RuleFor(x => x!.VitaminCMg).GreaterThanOrEqualTo(0).When(x => x!.VitaminCMg.HasValue);
                micros.RuleFor(x => x!.VitaminDIu).GreaterThanOrEqualTo(0).When(x => x!.VitaminDIu.HasValue);
                micros.RuleFor(x => x!.CalciumMg).GreaterThanOrEqualTo(0).When(x => x!.CalciumMg.HasValue);
                micros.RuleFor(x => x!.IronMg).GreaterThanOrEqualTo(0).When(x => x!.IronMg.HasValue);
                micros.RuleFor(x => x!.SodiumMg).GreaterThanOrEqualTo(0).When(x => x!.SodiumMg.HasValue);
            })
            .When(x => x.MicrosPer100 is not null);

        RuleFor(x => x.Measure)
            .ChildRules(measure =>
            {
                measure.RuleFor(x => x!.Label).NotEmpty().MaximumLength(80);
                measure.RuleFor(x => x!.GramsOrMl).GreaterThan(0);
            })
            .When(x => x.Measure is not null);
    }
}
