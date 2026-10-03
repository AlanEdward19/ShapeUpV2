using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Measurements.Shared;
using ShapeUp.Features.Nutrition.Measurements.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Measurements.GetMeasurements;

public class GetMeasurementsHandler(NutritionDbContext dbContext, INutritionAccessPolicy accessPolicy)
{
    public async Task<Result<MeasurementResponse[]>> HandleAsync(
        GetMeasurementsQuery query,
        int actorUserId,
        int targetUserId,
        CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<MeasurementResponse[]>.Failure(CommonErrors.Forbidden("You are not allowed to read this user's measurements."));

        if (query.From.HasValue && query.To.HasValue && query.From > query.To)
            return Result<MeasurementResponse[]>.Failure(CommonErrors.Validation("'from' must not be after 'to'."));

        var measurements = dbContext.Measurements.AsNoTracking().Where(m => m.UserId == targetUserId);
        if (query.From.HasValue)
            measurements = measurements.Where(m => m.Date >= query.From.Value);
        if (query.To.HasValue)
            measurements = measurements.Where(m => m.Date <= query.To.Value);

        var rows = await measurements
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .ToListAsync(cancellationToken);

        return Result<MeasurementResponse[]>.Success(rows.Select(MeasurementMapper.ToResponse).ToArray());
    }
}
