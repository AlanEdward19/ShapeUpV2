using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Diary.Shared;
using ShapeUp.Features.Nutrition.Diary.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Diary.RemoveDiaryEntry;

public class RemoveDiaryEntryHandler(
    NutritionDbContext dbContext,
    IValidator<RemoveDiaryEntryCommand> validator)
{
    public async Task<Result<DiaryDayResponse>> HandleAsync(
        RemoveDiaryEntryCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<DiaryDayResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var day = await dbContext.DiaryDays
            .Include(d => d.Entries)
            .FirstOrDefaultAsync(d => d.UserId == actorUserId && d.Date == command.Date, cancellationToken);

        if (day is null)
            return Result<DiaryDayResponse>.Failure(NutritionErrors.DiaryEntryNotFound(command.EntryId));

        var entry = day.Entries.FirstOrDefault(e => e.Id == command.EntryId);
        if (entry is null)
            return Result<DiaryDayResponse>.Failure(NutritionErrors.DiaryEntryNotFound(command.EntryId));

        day.Entries.Remove(entry);
        dbContext.DiaryEntries.Remove(entry);

        if (day.Entries.Count == 0)
            dbContext.DiaryDays.Remove(day);

        await dbContext.SaveChangesAsync(cancellationToken);

        if (day.Entries.Count == 0)
            return Result<DiaryDayResponse>.Success(DiaryMapper.ToResponse(command.Date, []));

        return Result<DiaryDayResponse>.Success(DiaryMapper.ToResponse(day));
    }
}
