using System.Text.Json;
using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Meals;

public record SaveUserMealCommand(string Id, SaveUserMealBody Body);

public partial class SaveUserMealCommandValidator : AbstractValidator<SaveUserMealCommand>
{
    public const int MaxItemsJsonLength = 200_000;

    public SaveUserMealCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty().MaximumLength(24);
        RuleFor(x => x.Body).NotNull();
        RuleFor(x => x.Body.Name).NotEmpty().MaximumLength(80)
            .When(x => x.Body is not null);
        RuleFor(x => x.Body.Time)
            .Must(t => string.IsNullOrWhiteSpace(t) || TimeRegex().IsMatch(t))
            .WithMessage("Time must be HH:mm (00:00 to 23:59).")
            .When(x => x.Body is not null);
        RuleFor(x => x.Body.WeekDays)
            .Must(days => days is null || days.All(d => d is >= 1 and <= 7))
            .WithMessage("WeekDays must be ISO weekdays from 1 (Monday) to 7 (Sunday).")
            .When(x => x.Body is not null);
        RuleFor(x => x.Body.EndDate)
            .GreaterThanOrEqualTo(x => x.Body.StartDate!.Value)
            .WithMessage("EndDate cannot be before StartDate.")
            .When(x => x.Body?.StartDate is not null && x.Body.EndDate is not null);
        RuleFor(x => x.Body.Order).GreaterThanOrEqualTo(0)
            .When(x => x.Body?.Order is not null);
        RuleFor(x => x.Body.TargetProteinG).InclusiveBetween(0, 10_000).When(x => x.Body?.TargetProteinG is not null);
        RuleFor(x => x.Body.TargetCarbG).InclusiveBetween(0, 10_000).When(x => x.Body?.TargetCarbG is not null);
        RuleFor(x => x.Body.TargetFatG).InclusiveBetween(0, 10_000).When(x => x.Body?.TargetFatG is not null);
        RuleFor(x => x.Body.ItemsJson)
            .Must(BeJsonArrayWithinLimit)
            .WithMessage("ItemsJson must be a JSON array of at most 200000 characters.")
            .When(x => x.Body?.ItemsJson is not null);
    }

    private static bool BeJsonArrayWithinLimit(string? json)
    {
        if (json is null) return true;
        if (json.Length > MaxItemsJsonLength) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.ValueKind == JsonValueKind.Array;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^([01]\d|2[0-3]):[0-5]\d$")]
    private static partial Regex TimeRegex();
}

public class SaveUserMealHandler(
    NutritionDbContext dbContext,
    IValidator<SaveUserMealCommand> validator)
{
    public const int MaxMealsPerUser = 50;

    /// <summary>Idempotent upsert by the client-generated id, so an offline retry is safe.</summary>
    public async Task<Result<UserMealDto>> HandleAsync(SaveUserMealCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<UserMealDto>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var meals = await dbContext.UserMeals.Where(m => m.UserId == actorUserId).ToListAsync(cancellationToken);
        var meal = meals.FirstOrDefault(m => m.Id == command.Id);

        if (meal is null)
        {
            if (await dbContext.UserMeals.AnyAsync(m => m.Id == command.Id, cancellationToken))
                return Result<UserMealDto>.Failure(CommonErrors.Conflict($"Meal id '{command.Id}' is already in use."));
            if (meals.Count >= MaxMealsPerUser)
                return Result<UserMealDto>.Failure(CommonErrors.Validation($"At most {MaxMealsPerUser} meals are allowed."));

            meal = new UserMeal
            {
                Id = command.Id,
                UserId = actorUserId,
                SortOrder = command.Body.Order ?? (meals.Count == 0 ? 0 : meals.Max(m => m.SortOrder) + 1)
            };
            dbContext.UserMeals.Add(meal);
        }
        else if (command.Body.Order is { } order)
        {
            meal.SortOrder = order;
        }

        UserMealMapper.Apply(meal, command.Body);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<UserMealDto>.Success(UserMealMapper.ToDto(meal));
    }
}
