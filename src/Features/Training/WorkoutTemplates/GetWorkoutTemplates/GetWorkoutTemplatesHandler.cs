using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.WorkoutTemplates.Shared;
using ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates.GetWorkoutTemplates;

public class GetWorkoutTemplatesHandler(IWorkoutTemplateRepository workoutTemplateRepository)
{
    public async Task<Result<KeysetPageResponse<WorkoutTemplateResponse>>> HandleAsync(
        GetWorkoutTemplatesQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        DateTime? createdBeforeUtc = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded))
                return Result<KeysetPageResponse<WorkoutTemplateResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));

            createdBeforeUtc = DateTime.FromBinary(decoded);
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var templates = await workoutTemplateRepository.GetByCreatorKeysetAsync(actorUserId, createdBeforeUtc, pageSize, cancellationToken);

        var items = templates.Select(x => x.ToResponse()).ToArray();
        var nextCursor = items.Length < pageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].CreatedAtUtc.ToBinary());
        return Result<KeysetPageResponse<WorkoutTemplateResponse>>.Success(new KeysetPageResponse<WorkoutTemplateResponse>(items, nextCursor));
    }
}

