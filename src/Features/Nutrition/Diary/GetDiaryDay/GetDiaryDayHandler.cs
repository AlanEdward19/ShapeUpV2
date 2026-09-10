using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Diary.Shared;
using ShapeUp.Features.Nutrition.Diary.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Diary.GetDiaryDay;

public class GetDiaryDayHandler(
    NutritionDbContext dbContext,
    IValidator<GetDiaryDayQuery> validator)
{
    public async Task<Result<DiaryDayResponse>> HandleAsync(
        GetDiaryDayQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(query, cancellationToken);
        if (!validation.IsValid)
            return Result<DiaryDayResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var day = await dbContext.DiaryDays
            .AsNoTracking()
            .Include(d => d.Entries)
            .FirstOrDefaultAsync(d => d.UserId == actorUserId && d.Date == query.Date, cancellationToken);

        if (day is null)
            return Result<DiaryDayResponse>.Success(DiaryMapper.ToResponse(query.Date, []));

        return Result<DiaryDayResponse>.Success(DiaryMapper.ToResponse(day));
    }
}
