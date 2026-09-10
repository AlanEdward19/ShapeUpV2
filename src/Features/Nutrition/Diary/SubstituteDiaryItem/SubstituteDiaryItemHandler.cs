using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Diary.AddDiaryEntry;
using ShapeUp.Features.Nutrition.Diary.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Diary.SubstituteDiaryItem;

public class SubstituteDiaryItemHandler(
    NutritionDbContext dbContext,
    AddDiaryEntryHandler addDiaryEntryHandler,
    IValidator<SubstituteDiaryItemCommand> validator)
{
    public async Task<Result<DiaryDayResponse>> HandleAsync(
        SubstituteDiaryItemCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<DiaryDayResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var day = await dbContext.DiaryDays
            .AsNoTracking()
            .Include(d => d.Entries)
            .FirstOrDefaultAsync(d => d.UserId == actorUserId && d.Date == command.Date, cancellationToken);

        var entry = day?.Entries.FirstOrDefault(e => e.Id == command.EntryId);
        if (entry is null)
            return Result<DiaryDayResponse>.Failure(NutritionErrors.DiaryEntryNotFound(command.EntryId));

        return await addDiaryEntryHandler.HandleAsync(
            new AddDiaryEntryCommand(
                command.EntryId,
                command.Date,
                entry.MealSlot,
                command.ReplacementFoodId,
                command.QuantityGramsOrMl),
            actorUserId,
            cancellationToken);
    }
}
