using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Comments.AddComment;
using ShapeUp.Features.Nutrition.Comments.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Comments.ManageComment;

/// <summary>The author edits (text only) or deletes their own comment, while the link with the client is still active.</summary>
public class ManageDiaryCommentHandler(
    NutritionDbContext dbContext,
    INutritionAccessPolicy accessPolicy,
    IUserRepository userRepository,
    IValidator<EditDiaryCommentCommand> validator)
{
    public async Task<Result<DiaryCommentResponse>> EditAsync(
        int commentId, EditDiaryCommentCommand command, int actorUserId, int clientUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<DiaryCommentResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var found = await FindAsync(commentId, actorUserId, clientUserId, cancellationToken);
        if (found.IsFailure)
            return Result<DiaryCommentResponse>.Failure(found.Error!);

        var comment = found.Value!;
        comment.Text = command.Text.Trim();
        await dbContext.SaveChangesAsync(cancellationToken);

        var author = await userRepository.GetByIdAsync(actorUserId, cancellationToken);
        return Result<DiaryCommentResponse>.Success(new DiaryCommentResponse(
            comment.Id, comment.Date, comment.EntryId, comment.AuthorUserId, author?.DisplayName, comment.Text, comment.CreatedAtUtc));
    }

    public async Task<Result> DeleteAsync(int commentId, int actorUserId, int clientUserId, CancellationToken cancellationToken)
    {
        var found = await FindAsync(commentId, actorUserId, clientUserId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error!);

        dbContext.DiaryComments.Remove(found.Value!);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result<NutritionDiaryComment>> FindAsync(
        int commentId, int actorUserId, int clientUserId, CancellationToken cancellationToken)
    {
        if (actorUserId == clientUserId
            || !await accessPolicy.CanManageNutritionForAsync(actorUserId, clientUserId, cancellationToken))
            return Result<NutritionDiaryComment>.Failure(CommonErrors.Forbidden("You are not allowed to change comments on this user's diary."));

        var comment = await dbContext.DiaryComments.FirstOrDefaultAsync(
            c => c.Id == commentId && c.ClientUserId == clientUserId, cancellationToken);
        if (comment is null)
            return Result<NutritionDiaryComment>.Failure(CommonErrors.NotFound($"Comment {commentId} was not found."));

        if (comment.AuthorUserId != actorUserId)
            return Result<NutritionDiaryComment>.Failure(CommonErrors.Forbidden("Only the author can change or delete a comment."));

        return Result<NutritionDiaryComment>.Success(comment);
    }
}

/// <summary>Only the text can change: the day and entry a comment is attached to are fixed.</summary>
public record EditDiaryCommentCommand(string Text);

public class EditDiaryCommentCommandValidator : AbstractValidator<EditDiaryCommentCommand>
{
    public EditDiaryCommentCommandValidator()
    {
        RuleFor(x => x.Text).NotEmpty().MaximumLength(1000)
            .Must(t => !ShapeUp.Shared.Safety.ObjectionableText.Contains(t)).WithMessage("Text contains language that is not allowed.");
    }
}
