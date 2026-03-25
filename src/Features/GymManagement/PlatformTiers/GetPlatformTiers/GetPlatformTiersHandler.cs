namespace ShapeUp.Features.GymManagement.PlatformTiers.GetPlatformTiers;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetPlatformTiersHandler(IPlatformTierRepository repository)
{
    public async Task<Result<KeysetPageResponse<GetPlatformTierResponse>>> HandleAsync(GetPlatformTiersQuery query, CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetPlatformTierResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var request = new KeysetPageRequest(query.Cursor, query.PageSize);
        var pageSize = request.NormalizePageSize();
        var tiers = await repository.GetAllKeysetAsync(lastId, pageSize, cancellationToken);

        var items = tiers.Select(t => new GetPlatformTierResponse(t.Id, t.Name, t.Description, t.Price, t.MaxClients, t.MaxTrainers, t.IsActive)).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);

        return Result<KeysetPageResponse<GetPlatformTierResponse>>.Success(new KeysetPageResponse<GetPlatformTierResponse>(items, nextCursor));
    }
}

