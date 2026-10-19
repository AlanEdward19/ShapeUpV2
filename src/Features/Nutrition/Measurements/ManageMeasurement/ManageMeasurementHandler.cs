using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Measurements.AddMeasurement;
using ShapeUp.Features.Nutrition.Measurements.Shared;
using ShapeUp.Features.Nutrition.Measurements.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Measurements.ManageMeasurement;

/// <summary>
/// Edit and delete of a measurement. Whoever recorded it owns it: the nutritionist keeps theirs, the client keeps their own,
/// and neither rewrites the other's. The actor must still be allowed to manage nutrition for the user (link revoked = no access).
/// </summary>
public class ManageMeasurementHandler(
    NutritionDbContext dbContext,
    INutritionAccessPolicy accessPolicy,
    IValidator<AddMeasurementCommand> validator)
{
    public async Task<Result<MeasurementResponse>> EditAsync(
        int measurementId, AddMeasurementCommand command, int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MeasurementResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var found = await FindAsync(measurementId, actorUserId, targetUserId, cancellationToken);
        if (found.IsFailure)
            return Result<MeasurementResponse>.Failure(found.Error!);

        var measurement = found.Value!;
        measurement.Date = command.Date;
        measurement.WeightKg = command.WeightKg;
        measurement.HeightCm = command.HeightCm;
        measurement.BodyFatPercent = command.BodyFatPercent;
        measurement.WaistCm = command.WaistCm;
        measurement.HipCm = command.HipCm;
        measurement.Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<MeasurementResponse>.Success(MeasurementMapper.ToResponse(measurement));
    }

    public async Task<Result> DeleteAsync(int measurementId, int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        var found = await FindAsync(measurementId, actorUserId, targetUserId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error!);

        dbContext.Measurements.Remove(found.Value!);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<NutritionMeasurement>> FindAsync(
        int measurementId, int actorUserId, int targetUserId, CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<NutritionMeasurement>.Failure(
                CommonErrors.Forbidden("You are not allowed to change measurements for this user."));

        var measurement = await dbContext.Measurements.FirstOrDefaultAsync(
            m => m.Id == measurementId && m.UserId == targetUserId, cancellationToken);
        if (measurement is null)
            return Result<NutritionMeasurement>.Failure(CommonErrors.NotFound($"Measurement {measurementId} was not found."));

        if (measurement.RecordedByUserId != actorUserId)
            return Result<NutritionMeasurement>.Failure(
                CommonErrors.Forbidden("Only who recorded a measurement can change or delete it."));

        return Result<NutritionMeasurement>.Success(measurement);
    }
}
