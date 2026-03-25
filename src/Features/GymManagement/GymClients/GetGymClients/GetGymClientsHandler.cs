namespace ShapeUp.Features.GymManagement.GymClients.GetGymClients;

using Shared.Abstractions;
using Shared.Errors;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetGymClientsHandler(IGymClientRepository repository, IGymRepository gymRepository)
{
    public async Task<Result<KeysetPageResponse<GetGymClientResponse>>> HandleAsync(GetGymClientsQuery query, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(query.GymId, cancellationToken);
        if (gym is null) return Result<KeysetPageResponse<GetGymClientResponse>>.Failure(GymManagementErrors.GymNotFound(query.GymId));

        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetGymClientResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();

        var clients = query.TrainerId.HasValue
            ? await repository.GetByTrainerIdKeysetAsync(query.GymId, query.TrainerId.Value, lastId, pageSize, cancellationToken)
            : await repository.GetByGymIdKeysetAsync(query.GymId, lastId, pageSize, cancellationToken);

        var items = clients.Select(c => new GetGymClientResponse(c.Id, c.GymId, c.UserId, c.GymPlan?.Name ?? string.Empty, c.TrainerId, c.EnrolledAt)).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<GetGymClientResponse>>.Success(new KeysetPageResponse<GetGymClientResponse>(items, nextCursor));
    }
}

