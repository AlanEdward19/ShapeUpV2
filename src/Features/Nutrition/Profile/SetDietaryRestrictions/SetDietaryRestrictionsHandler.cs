using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Profile.Shared;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Profile.SetDietaryRestrictions;

/// <summary>Replaces the restrictions and allergies of <c>userId</c>; blank text clears the field.</summary>
public class SetDietaryRestrictionsHandler(
    NutritionDbContext dbContext,
    IValidator<SetDietaryRestrictionsCommand> validator)
{
    public async Task<Result<NutritionProfileResponse>> HandleAsync(
        SetDietaryRestrictionsCommand command,
        int userId,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<NutritionProfileResponse>.Failure(CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var profile = await dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);
        if (profile is null)
        {
            profile = new NutritionProfile { UserId = userId };
            dbContext.Profiles.Add(profile);
        }

        profile.Restrictions = Normalize(command.Restrictions);
        profile.Allergies = Normalize(command.Allergies);
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return Result<NutritionProfileResponse>.Success(NutritionProfileMapper.ToResponse(profile));
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
