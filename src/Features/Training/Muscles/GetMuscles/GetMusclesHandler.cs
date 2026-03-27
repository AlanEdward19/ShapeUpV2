using ShapeUp.Features.Training.Muscles.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Muscles.GetMuscles;

public class GetMusclesHandler(IMuscleRepository muscleRepository)
{
    public async Task<Result<KeysetPageResponse<MuscleResponse>>> HandleAsync(GetMusclesQuery query, CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<MuscleResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var normalizedPageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var items = (await muscleRepository.GetKeysetPageAsync(lastId, normalizedPageSize, cancellationToken))
            .Select(x => new MuscleResponse(x.Id, x.Name, x.NamePt))
            .ToArray();

        var nextCursor = items.Length < normalizedPageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);
        return Result<KeysetPageResponse<MuscleResponse>>.Success(new KeysetPageResponse<MuscleResponse>(items, nextCursor));
    }
}