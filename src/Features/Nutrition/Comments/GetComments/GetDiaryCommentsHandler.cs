using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Comments.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Comments.GetComments;

public class GetDiaryCommentsHandler(
    NutritionDbContext dbContext,
    INutritionAccessPolicy accessPolicy,
    IUserRepository userRepository)
{
    public async Task<Result<DiaryCommentResponse[]>> HandleAsync(
        GetDiaryCommentsQuery query,
        int actorUserId,
        int clientUserId,
        CancellationToken cancellationToken)
    {
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, clientUserId, cancellationToken))
            return Result<DiaryCommentResponse[]>.Failure(CommonErrors.Forbidden("You are not allowed to read this user's diary comments."));

        if (query.Date is null && (query.From is null || query.To is null))
            return Result<DiaryCommentResponse[]>.Failure(CommonErrors.Validation("Provide either 'date' or both 'from' and 'to'."));

        var from = query.Date ?? query.From!.Value;
        var to = query.Date ?? query.To!.Value;
        if (from > to)
            return Result<DiaryCommentResponse[]>.Failure(CommonErrors.Validation("'from' must not be after 'to'."));

        var comments = await dbContext.DiaryComments
            .AsNoTracking()
            .Where(c => c.ClientUserId == clientUserId && c.Date >= from && c.Date <= to)
            .OrderBy(c => c.Date)
            .ThenBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var names = new Dictionary<int, string?>();
        foreach (var authorId in comments.Select(c => c.AuthorUserId).Distinct())
            names[authorId] = (await userRepository.GetByIdAsync(authorId, cancellationToken))?.DisplayName;

        return Result<DiaryCommentResponse[]>.Success(comments
            .Select(c => new DiaryCommentResponse(c.Id, c.Date, c.EntryId, c.AuthorUserId, names[c.AuthorUserId], c.Text, c.CreatedAtUtc))
            .ToArray());
    }
}
