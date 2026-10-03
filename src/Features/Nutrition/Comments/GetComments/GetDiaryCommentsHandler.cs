using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Comments.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Comments.GetComments;

public class GetDiaryCommentsHandler(
    NutritionDbContext dbContext,
    INutritionAccessPolicy accessPolicy,
    IUserRepository userRepository)
{
    public const int MaxRangeDays = 366;

    public async Task<Result<KeysetPageResponse<DiaryCommentResponse>>> HandleAsync(
        GetDiaryCommentsQuery query,
        int actorUserId,
        int clientUserId,
        CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, clientUserId, cancellationToken))
            return Result<KeysetPageResponse<DiaryCommentResponse>>.Failure(CommonErrors.Forbidden("You are not allowed to read this user's diary comments."));

        if (query.Date is null && (query.From is null || query.To is null))
            return Result<KeysetPageResponse<DiaryCommentResponse>>.Failure(CommonErrors.Validation("Provide either 'date' or both 'from' and 'to'."));

        var from = query.Date ?? query.From!.Value;
        var to = query.Date ?? query.To!.Value;
        if (from > to)
            return Result<KeysetPageResponse<DiaryCommentResponse>>.Failure(CommonErrors.Validation("'from' must not be after 'to'."));
        if (to.DayNumber - from.DayNumber + 1 > MaxRangeDays)
            return Result<KeysetPageResponse<DiaryCommentResponse>>.Failure(CommonErrors.Validation($"The range cannot exceed {MaxRangeDays} days."));

        var comments = dbContext.DiaryComments
            .AsNoTracking()
            .Where(c => c.ClientUserId == clientUserId && c.Date >= from && c.Date <= to);

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!DateIdCursor.TryDecode(query.Cursor, out var cursorDate, out var cursorId))
                return Result<KeysetPageResponse<DiaryCommentResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));
            comments = comments.Where(c => c.Date > cursorDate || (c.Date == cursorDate && c.Id > cursorId));
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();
        var rows = await comments
            .OrderBy(c => c.Date)
            .ThenBy(c => c.Id)
            .Take(pageSize + 1)
            .ToListAsync(cancellationToken);

        var page = rows.Take(pageSize).ToList();
        var nextCursor = rows.Count > pageSize ? DateIdCursor.Encode(page[^1].Date, page[^1].Id) : null;

        var names = (await userRepository.GetByIdsAsync(page.Select(c => c.AuthorUserId).Distinct().ToArray(), cancellationToken))
            .ToDictionary(u => u.Id, u => u.DisplayName);

        var items = page
            .Select(c => new DiaryCommentResponse(c.Id, c.Date, c.EntryId, c.AuthorUserId, names.GetValueOrDefault(c.AuthorUserId), c.Text, c.CreatedAtUtc))
            .ToArray();
        return Result<KeysetPageResponse<DiaryCommentResponse>>.Success(new KeysetPageResponse<DiaryCommentResponse>(items, nextCursor));
    }
}
