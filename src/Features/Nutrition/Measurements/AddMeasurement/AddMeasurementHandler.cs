using FluentValidation;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Measurements.Shared;
using ShapeUp.Features.Nutrition.Measurements.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Measurements.AddMeasurement;

public class AddMeasurementHandler(
    NutritionDbContext dbContext,
    INutritionAccessPolicy accessPolicy,
    IValidator<AddMeasurementCommand> validator)
{
    public async Task<Result<MeasurementResponse>> HandleAsync(
        AddMeasurementCommand command,
        int actorUserId,
        int targetUserId,
        CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<MeasurementResponse>.Failure(CommonErrors.Forbidden("You are not allowed to record measurements for this user."));

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MeasurementResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var measurement = new NutritionMeasurement
        {
            UserId = targetUserId,
            Date = command.Date,
            WeightKg = command.WeightKg,
            HeightCm = command.HeightCm,
            BodyFatPercent = command.BodyFatPercent,
            WaistCm = command.WaistCm,
            HipCm = command.HipCm,
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            RecordedByUserId = actorUserId,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Measurements.Add(measurement);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<MeasurementResponse>.Success(MeasurementMapper.ToResponse(measurement));
    }
}
