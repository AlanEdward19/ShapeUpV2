using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Profile.Shared;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Profile.SetManualGoal;

public class SetManualGoalHandler(
    NutritionDbContext dbContext,
    IValidator<SetManualGoalCommand> validator)
{
    public async Task<Result<NutritionProfileResponse>> HandleAsync(
        SetManualGoalCommand command,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<NutritionProfileResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var nowUtc = DateTime.UtcNow;
        var profile = await dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == actorUserId, cancellationToken);
        if (profile is null)
        {
            profile = new NutritionProfile
            {
                UserId = actorUserId,
                OnboardingSkipped = true
            };
            dbContext.Profiles.Add(profile);
        }
        else
        {
            profile.OnboardingSkipped = true;
        }

        profile.ActiveGoal = NutritionProfileMapper.ToMacroValueObject(command.Goal);
        profile.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<NutritionProfileResponse>.Success(NutritionProfileMapper.ToResponse(profile));
    }
}
