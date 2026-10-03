using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Comments.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Comments.AddComment;

/// <summary>Only the linked professional comments; users cannot comment on their own diary.</summary>
public class AddDiaryCommentHandler(
    NutritionDbContext dbContext,
    INutritionAccessPolicy accessPolicy,
    IUserRepository userRepository,
    IValidator<AddDiaryCommentCommand> validator)
{
    public async Task<Result<DiaryCommentResponse>> HandleAsync(
        AddDiaryCommentCommand command,
        int actorUserId,
        int clientUserId,
        CancellationToken cancellationToken)
    {
        if (actorUserId == clientUserId
            || !await accessPolicy.CanManageNutritionForAsync(actorUserId, clientUserId, cancellationToken))
            return Result<DiaryCommentResponse>.Failure(CommonErrors.Forbidden("You are not allowed to comment on this user's diary."));

        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<DiaryCommentResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var entryId = string.IsNullOrWhiteSpace(command.EntryId) ? null : command.EntryId.Trim();
        if (entryId is not null)
        {
            var entryExists = await dbContext.DiaryEntries.AsNoTracking().AnyAsync(
                e => e.Id == entryId && e.DiaryDay.UserId == clientUserId && e.DiaryDay.Date == command.Date,
                cancellationToken);
            if (!entryExists)
                return Result<DiaryCommentResponse>.Failure(NutritionErrors.DiaryEntryNotFound(entryId));
        }

        var comment = new NutritionDiaryComment
        {
            ClientUserId = clientUserId,
            Date = command.Date,
            EntryId = entryId,
            AuthorUserId = actorUserId,
            Text = command.Text.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };
        dbContext.DiaryComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);

        var author = await userRepository.GetByIdAsync(actorUserId, cancellationToken);
        return Result<DiaryCommentResponse>.Success(new DiaryCommentResponse(
            comment.Id, comment.Date, comment.EntryId, comment.AuthorUserId, author?.DisplayName, comment.Text, comment.CreatedAtUtc));
    }
}
