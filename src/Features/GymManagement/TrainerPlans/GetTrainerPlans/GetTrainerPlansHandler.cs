namespace ShapeUp.Features.GymManagement.TrainerPlans.GetTrainerPlans;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public record GetTrainerPlansQuery(int TrainerId, string? Cursor, int? PageSize);
public record GetTrainerPlanResponse(int Id, int TrainerId, string Name, string? Description, decimal Price, int DurationDays, bool IsActive);

public class GetTrainerPlansHandler(ITrainerPlanRepository repository)
{
    public async Task<Result<KeysetPageResponse<GetTrainerPlanResponse>>> HandleAsync(GetTrainerPlansQuery query, CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetTrainerPlanResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var plans = await repository.GetByTrainerIdKeysetAsync(query.TrainerId, lastId, pageSize, cancellationToken);
        var items = plans.Select(p => new GetTrainerPlanResponse(p.Id, p.TrainerId, p.Name, p.Description, p.Price, p.DurationDays, p.IsActive)).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<GetTrainerPlanResponse>>.Success(new KeysetPageResponse<GetTrainerPlanResponse>(items, nextCursor));
    }
}

