using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Measurements.Shared;
using ShapeUp.Features.Nutrition.Measurements.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Measurements.GetMeasurements;

public class GetMeasurementsHandler(NutritionDbContext dbContext, INutritionAccessPolicy accessPolicy)
{
    public const int MaxRangeDays = 366;

    public async Task<Result<KeysetPageResponse<MeasurementResponse>>> HandleAsync(
        GetMeasurementsQuery query,
        int actorUserId,
        int targetUserId,
        CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, targetUserId, cancellationToken))
            return Result<KeysetPageResponse<MeasurementResponse>>.Failure(CommonErrors.Forbidden("You are not allowed to read this user's measurements."));

        if (query.From.HasValue && query.To.HasValue)
        {
            if (query.From > query.To)
                return Result<KeysetPageResponse<MeasurementResponse>>.Failure(CommonErrors.Validation("'from' must not be after 'to'."));
            if (query.To.Value.DayNumber - query.From.Value.DayNumber + 1 > MaxRangeDays)
                return Result<KeysetPageResponse<MeasurementResponse>>.Failure(CommonErrors.Validation($"The range cannot exceed {MaxRangeDays} days."));
        }

        var measurements = dbContext.Measurements.AsNoTracking().Where(m => m.UserId == targetUserId);
        if (query.From.HasValue)
            measurements = measurements.Where(m => m.Date >= query.From.Value);
        if (query.To.HasValue)
            measurements = measurements.Where(m => m.Date <= query.To.Value);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!DateIdCursor.TryDecode(query.Cursor, out var cursorDate, out var cursorId))
                return Result<KeysetPageResponse<MeasurementResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            measurements = measurements.Where(m => m.Date < cursorDate || (m.Date == cursorDate && m.Id < cursorId));
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var rows = await measurements
            .OrderByDescending(m => m.Date)
            .ThenByDescending(m => m.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var page = rows.Take(pageSize).ToList();
        var nextCursor = rows.Count > pageSize ? DateIdCursor.Encode(page[^1].Date, page[^1].Id) : null;

        return Result<KeysetPageResponse<MeasurementResponse>>.Success(
            new KeysetPageResponse<MeasurementResponse>(page.Select(MeasurementMapper.ToResponse).ToArray(), nextCursor));
    }
}
