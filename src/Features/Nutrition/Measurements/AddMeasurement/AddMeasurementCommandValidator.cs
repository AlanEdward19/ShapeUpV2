using FluentValidation;

namespace ShapeUp.Features.Nutrition.Measurements.AddMeasurement;

public class AddMeasurementCommandValidator : AbstractValidator<AddMeasurementCommand>
{
    public AddMeasurementCommandValidator()
    {
        RuleFor(x => x.Date).NotEqual(default(DateOnly));
        RuleFor(x => x)
            .Must(x => x.WeightKg.HasValue || x.HeightCm.HasValue || x.BodyFatPercent.HasValue
                       || x.WaistCm.HasValue || x.HipCm.HasValue)
            .WithName("Measures")
            .WithMessage("Provide at least one measure.");
        RuleFor(x => x.WeightKg).InclusiveBetween(1m, 700m).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.HeightCm).InclusiveBetween(30m, 260m).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.BodyFatPercent).InclusiveBetween(1m, 80m).When(x => x.BodyFatPercent.HasValue);
        RuleFor(x => x.WaistCm).InclusiveBetween(10m, 400m).When(x => x.WaistCm.HasValue);
        RuleFor(x => x.HipCm).InclusiveBetween(10m, 400m).When(x => x.HipCm.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
