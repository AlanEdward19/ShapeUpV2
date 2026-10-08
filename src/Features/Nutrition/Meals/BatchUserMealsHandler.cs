using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Meals;

public class BatchUserMealsHandler(
    NutritionDbContext dbContext,
    IValidator<SaveUserMealCommand> validator)
{
    public async Task<Result<UserMealDto[]>> HandleAsync(BatchUserMealsBody? body, int actorUserId, CancellationToken cancellationToken)
    {
        if (body is null)
            return Fail(CommonErrors.Validation("Body is required."));

        var upserts = body.Upserts ?? [];
        var deleteIds = (body.DeleteIds ?? []).Distinct().ToArray();
        var order = (body.Order ?? []).Distinct().ToArray();
        var max = SaveUserMealHandler.MaxMealsPerUser;

        if (upserts.Length > max || deleteIds.Length > max || order.Length > max)
            return Fail(CommonErrors.Validation($"At most {max} meals per list."));
        if (upserts.Select(u => u.Id).Distinct().Count() != upserts.Length)
            return Fail(CommonErrors.Validation("Upserts contain a repeated meal id."));
        if (upserts.Any(u => deleteIds.Contains(u.Id)))
            return Fail(CommonErrors.Validation("A meal cannot be created/changed and deleted in the same batch."));

        var errors = new List<string>();
        foreach (var upsert in upserts)
        {
            var save = new SaveUserMealBody(
                upsert.Name, upsert.Time, upsert.WeekDays, null,
                upsert.TargetProteinG, upsert.TargetCarbG, upsert.TargetFatG, upsert.ItemsJson, upsert.StartDate, upsert.EndDate);
            var result = await validator.ValidateAsync(new SaveUserMealCommand(upsert.Id, save), cancellationToken);
            errors.AddRange(result.Errors.Select(e => $"{upsert.Id}: {e.ErrorMessage}"));
        }
        if (errors.Count > 0)
            return Fail(CommonErrors.Validation(string.Join("; ", errors)));

        var meals = await dbContext.UserMeals.Where(m => m.UserId == actorUserId).ToListAsync(cancellationToken);

        foreach (var gone in meals.Where(m => deleteIds.Contains(m.Id)).ToList())
        {
            dbContext.UserMeals.Remove(gone);
            meals.Remove(gone);
        }

        var newIds = upserts.Select(u => u.Id).Where(id => meals.All(m => m.Id != id)).ToArray();
        if (meals.Count + newIds.Length > max)
            return Fail(CommonErrors.Validation($"At most {max} meals are allowed."));
        if (newIds.Length > 0 && await dbContext.UserMeals.AnyAsync(m => newIds.Contains(m.Id), cancellationToken))
            return Fail(CommonErrors.Conflict("A meal id is already in use."));

        var next = meals.Count == 0 ? 0 : meals.Max(m => m.SortOrder) + 1;
        foreach (var upsert in upserts)
        {
            var meal = meals.FirstOrDefault(m => m.Id == upsert.Id);
            if (meal is null)
            {
                meal = new UserMeal { Id = upsert.Id, UserId = actorUserId, SortOrder = next++ };
                dbContext.UserMeals.Add(meal);
                meals.Add(meal);
            }

            UserMealMapper.Apply(meal, new SaveUserMealBody(
                upsert.Name, upsert.Time, upsert.WeekDays, null,
                upsert.TargetProteinG, upsert.TargetCarbG, upsert.TargetFatG, upsert.ItemsJson, upsert.StartDate, upsert.EndDate));
        }

        // The listed ids come first, in that order; the rest keep their current relative order.
        var current = meals.OrderBy(m => m.SortOrder).ThenBy(m => m.Name).ToList();
        var byId = current.ToDictionary(m => m.Id);
        var ordered = order.Where(byId.ContainsKey).Select(id => byId[id]).ToList();
        if (order.Length > 0)
            ordered.AddRange(current.Where(m => !ordered.Contains(m)));
        else
            ordered = current;
        for (var i = 0; i < ordered.Count; i++)
            ordered[i].SortOrder = i;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<UserMealDto[]>.Success(ordered.Select(UserMealMapper.ToDto).ToArray());
    }

    private static Result<UserMealDto[]> Fail(Error error) => Result<UserMealDto[]>.Failure(error);
}
