namespace ShapeUp.Features.GymManagement.GymPlans.GetGymPlans;

using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetGymPlansHandler(IGymPlanRepository repository, IGymRepository gymRepository)
{
    public async Task<Result<KeysetPageResponse<GetGymPlanResponse>>> HandleAsync(GetGymPlansQuery query, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(query.GymId, cancellationToken);
        if (gym is null) return Result<KeysetPageResponse<GetGymPlanResponse>>.Failure(GymManagementErrors.GymNotFound(query.GymId));

        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetGymPlanResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var plans = await repository.GetByGymIdKeysetAsync(query.GymId, lastId, pageSize, cancellationToken);
        var items = plans.Select(p => new GetGymPlanResponse(p.Id, p.GymId, p.Name, p.Description, p.Price, p.DurationDays, p.IsActive)).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<GetGymPlanResponse>>.Success(new KeysetPageResponse<GetGymPlanResponse>(items, nextCursor));
    }
}

