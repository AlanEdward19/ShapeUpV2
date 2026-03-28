using ShapeUp.Features.Training.Exercises.CreateExercise;
using ShapeUp.Features.Training.Exercises.Shared.ViewModels;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.Exercises.GetExercises;

public class GetExercisesHandler(IExerciseRepository exerciseRepository)
{
    public async Task<Result<KeysetPageResponse<ExerciseResponse>>> HandleAsync(GetExercisesQuery query, CancellationToken cancellationToken)
    {
        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<ExerciseResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var normalizedPageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var exercises = await exerciseRepository.GetKeysetPageAsync(lastId, normalizedPageSize, cancellationToken);
        var items = exercises.Select(CreateExerciseHandler.MapResponse).ToArray();
        var nextCursor = items.Length < normalizedPageSize ? null : KeysetCursorCodec.EncodeLong(items[^1].Id);

        return Result<KeysetPageResponse<ExerciseResponse>>.Success(new KeysetPageResponse<ExerciseResponse>(items, nextCursor));
    }
}