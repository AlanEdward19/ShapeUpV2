using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Profile.Shared;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;

public class CompleteOnboardingHandler(
    NutritionDbContext dbContext,
    IWeightTrackingRepository weightTrackingRepository,
    IValidator<CompleteOnboardingCommand> validator)
{
    public async Task<Result<NutritionProfileResponse>> HandleAsync(
        CompleteOnboardingCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<NutritionProfileResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var currentWeight = await GetLatestWeightKgAsync(actorUserId, cancellationToken);
        if (currentWeight is null)
            return Result<NutritionProfileResponse>.Failure(NutritionErrors.WeightNotRegistered());

        var nowUtc = DateTime.UtcNow;
        var profile = await dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == actorUserId, cancellationToken);
        if (profile is null)
        {
            profile = new NutritionProfile { UserId = actorUserId };
            dbContext.Profiles.Add(profile);
        }

        profile.HeightCm = command.HeightCm;
        profile.Age = command.Age;
        profile.BiologicalSex = NormalizeSex(command.BiologicalSex);
        profile.ActivityLevel = NormalizeActivityLevel(command.ActivityLevel);
        profile.OnboardingSkipped = false;

        MacroGoal goal;
        try
        {
            goal = TdeeCalculator.Calculate(profile, currentWeight.Value);
        }
        catch (InvalidOperationException ex)
        {
            return Result<NutritionProfileResponse>.Failure(CommonErrors.Validation(ex.Message));
        }

        profile.ActiveGoal = NutritionProfileMapper.ToMacroValueObject(goal);
        profile.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<NutritionProfileResponse>.Success(NutritionProfileMapper.ToResponse(profile));
    }

    private async Task<decimal?> GetLatestWeightKgAsync(int userId, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var registers = await weightTrackingRepository.GetRegistersByRangeAsync(
            userId,
            today.AddYears(-1),
            today,
            cancellationToken);

        if (registers.Count == 0)
            return null;

        return registers
            .OrderByDescending(r => r.Day, StringComparer.Ordinal)
            .First()
            .Weight;
    }

    private static string NormalizeSex(string biologicalSex) =>
        string.Equals(biologicalSex, "Female", StringComparison.OrdinalIgnoreCase) ? "Female" : "Male";

    private static string NormalizeActivityLevel(string activityLevel) =>
        activityLevel switch
        {
            _ when string.Equals(activityLevel, "Light", StringComparison.OrdinalIgnoreCase) => "Light",
            _ when string.Equals(activityLevel, "Moderate", StringComparison.OrdinalIgnoreCase) => "Moderate",
            _ when string.Equals(activityLevel, "Active", StringComparison.OrdinalIgnoreCase) => "Active",
            _ when string.Equals(activityLevel, "VeryActive", StringComparison.OrdinalIgnoreCase) => "VeryActive",
            _ => "Sedentary"
        };
}
