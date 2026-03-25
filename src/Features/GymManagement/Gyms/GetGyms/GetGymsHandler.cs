namespace ShapeUp.Features.GymManagement.Gyms.GetGyms;

using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Errors;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetGymsHandler(IGymRepository repository)
{
    public async Task<Result<KeysetPageResponse<GetGymResponse>>> HandleAsync(GetGymsQuery query, CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetGymResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var request = new KeysetPageRequest(query.Cursor, query.PageSize);
        var pageSize = request.NormalizePageSize();

        var gyms = query.OwnerId.HasValue
            ? await repository.GetByOwnerIdKeysetAsync(query.OwnerId.Value, lastId, pageSize, cancellationToken)
            : await repository.GetAllKeysetAsync(lastId, pageSize, cancellationToken);

        var items = gyms.Select(g => new GetGymResponse(
            g.Id, g.OwnerId, g.Name, g.Description, g.Address, g.PlatformTier?.Name, g.CreatedAt)).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);

        return Result<KeysetPageResponse<GetGymResponse>>.Success(new KeysetPageResponse<GetGymResponse>(items, nextCursor));
    }

    public async Task<Result<GetGymResponse>> GetByIdAsync(int gymId, CancellationToken cancellationToken)
    {
        var gym = await repository.GetByIdAsync(gymId, cancellationToken);
        if (gym is null) return Result<GetGymResponse>.Failure(GymManagementErrors.GymNotFound(gymId));
        return Result<GetGymResponse>.Success(
            new GetGymResponse(gym.Id, gym.OwnerId, gym.Name, gym.Description, gym.Address, gym.PlatformTier?.Name, gym.CreatedAt));
    }
}

