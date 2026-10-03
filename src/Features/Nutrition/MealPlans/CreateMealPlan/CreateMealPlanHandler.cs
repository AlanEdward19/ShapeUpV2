using FluentValidation;
using MongoDB.Bson;
using MongoDB.Driver;
using ShapeUp.Features.Nutrition.MealPlans.Shared;
using ShapeUp.Features.Nutrition.MealPlans.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.MealPlans.CreateMealPlan;

public class CreateMealPlanHandler(
    IMealPlanRepository mealPlanRepository,
    IFoodRepository foodRepository,
    INutritionAccessPolicy accessPolicy,
    IValidator<CreateMealPlanCommand> validator)
{
    public async Task<Result<MealPlanResponse>> HandleAsync(
        CreateMealPlanCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<MealPlanResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var ownerUserId = command.TargetUserId ?? actorUserId;
        if (!await accessPolicy.CanManageNutritionForAsync(actorUserId, ownerUserId, cancellationToken))
            return Result<MealPlanResponse>.Failure(CommonErrors.Forbidden("You are not allowed to create a meal plan for this user."));

        var prescribedBy = ownerUserId == actorUserId ? (int?)null : actorUserId;
        var requestedId = command.Id?.ToLowerInvariant();
        if (requestedId is not null)
        {
            var existing = await mealPlanRepository.GetByIdAsync(requestedId, cancellationToken);
            if (existing is not null)
                return Reuse(existing, ownerUserId, prescribedBy, requestedId);
        }

        foreach (var item in command.Items)
        {
            if (await foodRepository.GetByIdAsync(item.FoodId, cancellationToken) is null)
                return Result<MealPlanResponse>.Failure(NutritionErrors.FoodNotFound(item.FoodId));
        }

        var nowUtc = DateTime.UtcNow;
        var plan = new MealPlanDocument
        {
            Id = requestedId ?? ObjectId.GenerateNewId().ToString(),
            UserId = ownerUserId,
            PrescribedByUserId = prescribedBy,
            Name = command.Name.Trim(),
            PrescribedByRelationshipId = null,
            IsActive = false,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Items = command.Items
                .Select(i => new MealPlanItemDocument
                {
                    MealSlot = MealPlanMapper.NormalizeMealSlot(i.MealSlot),
                    FoodId = i.FoodId,
                    QuantityGramsOrMl = i.QuantityGramsOrMl
                })
                .ToList()
        };

        try
        {
            await mealPlanRepository.CreateAsync(plan, cancellationToken);
        }
        catch (MongoWriteException ex) when (requestedId is not null && ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            // A concurrent resend inserted the same id first: answer as if it had been there already.
            var existing = await mealPlanRepository.GetByIdAsync(requestedId, cancellationToken);
            if (existing is not null)
                return Reuse(existing, ownerUserId, prescribedBy, requestedId);
            throw;
        }

        return Result<MealPlanResponse>.Success(MealPlanMapper.ToResponse(plan));
    }

    private static Result<MealPlanResponse> Reuse(MealPlanDocument existing, int ownerUserId, int? prescribedBy, string id) =>
        existing.UserId == ownerUserId && existing.PrescribedByUserId == prescribedBy
            ? Result<MealPlanResponse>.Success(MealPlanMapper.ToResponse(existing))
            : Result<MealPlanResponse>.Failure(CommonErrors.Conflict($"Meal plan id '{id}' is already in use."));
}
