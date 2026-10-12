using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Meals;

public class ListUserMealsHandler(NutritionDbContext dbContext)
{
    public async Task<Result<UserMealDto[]>> HandleAsync(int actorUserId, CancellationToken cancellationToken)
    {
        var meals = await dbContext.UserMeals
            .AsNoTracking()
            .Where(m => m.UserId == actorUserId)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .ToListAsync(cancellationToken);
        return Result<UserMealDto[]>.Success(meals.Select(UserMealMapper.ToDto).ToArray());
    }
}

public class DeleteUserMealHandler(NutritionDbContext dbContext)
{
    /// <summary>Diary entries keep their <c>MealName</c>, so past days still read right.</summary>
    public async Task<Result> HandleAsync(string mealId, int actorUserId, CancellationToken cancellationToken)
    {
        var meal = await dbContext.UserMeals.FirstOrDefaultAsync(m => m.Id == mealId && m.UserId == actorUserId, cancellationToken);
        if (meal is null)
            return Result.Failure(NutritionErrors.UserMealNotFound(mealId));

        dbContext.UserMeals.Remove(meal);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
