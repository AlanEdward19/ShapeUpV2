using FluentValidation;
using ShapeUp.Features.Training.ExerciseRequests.Shared;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.ExerciseRequests.ListExerciseRequests;

/// <summary>Keyset page, newest first. <c>ownerOnly</c> lists the caller's own requests; otherwise it is the admin list.</summary>
public class ListExerciseRequestsHandler(
    IExerciseRequestRepository repository,
    ExerciseRequestResponseBuilder responseBuilder,
    IValidator<ListExerciseRequestsQuery> validator)
{
    public async Task<Result<KeysetPageResponse<ExerciseRequestResponse>>> HandleAsync(
        ListExerciseRequestsQuery query,
        int actorUserId,
        bool ownerOnly,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<KeysetPageResponse<ExerciseRequestResponse>>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        int? lastId = null;
        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!KeysetCursorCodec.TryDecodeLong(query.Cursor, out var decoded) || decoded <= 0 || decoded > int.MaxValue)
                return Result<KeysetPageResponse<ExerciseRequestResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            lastId = (int)decoded;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var rows = await repository.GetKeysetPageAsync(
            ownerOnly ? actorUserId : null,
            ListExerciseRequestsQueryValidator.Normalize(query.Status),
            lastId, pageSize, cancellationToken);

        var items = await responseBuilder.BuildAsync(rows, includeReviewer: !ownerOnly, cancellationToken);
        var nextCursor = rows.Count < pageSize ? null : KeysetCursorCodec.EncodeLong(rows[^1].Id);
        return Result<KeysetPageResponse<ExerciseRequestResponse>>.Success(new KeysetPageResponse<ExerciseRequestResponse>(items, nextCursor));
    }
}
