namespace ShapeUp.Features.GymManagement.TrainerClients.GetTrainerClients;

using Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public class GetTrainerClientsHandler(ITrainerClientRepository repository)
{
    public async Task<Result<KeysetPageResponse<GetTrainerClientResponse>>> HandleAsync(GetTrainerClientsQuery query, CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<GetTrainerClientResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var clients = await repository.GetByTrainerIdKeysetAsync(query.TrainerId, lastId, pageSize, cancellationToken);
        var items = clients.Select(c => new GetTrainerClientResponse(c.Id, c.TrainerId, c.ClientId, c.TrainerPlan?.Name ?? string.Empty, c.EnrolledAt)).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<GetTrainerClientResponse>>.Success(new KeysetPageResponse<GetTrainerClientResponse>(items, nextCursor));
    }
}

