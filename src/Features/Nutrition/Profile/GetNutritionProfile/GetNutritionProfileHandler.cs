using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Profile.Shared;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Nutrition.Profile.GetNutritionProfile;

public class GetNutritionProfileHandler(NutritionDbContext dbContext, INutritionistLinkReader linkReader)
{
    public async Task<Result<NutritionProfileResponse>> HandleAsync(
        GetNutritionProfileQuery query,
        int actorUserId,
        CancellationToken cancellationToken)
    {
        var profile = await dbContext.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == actorUserId, cancellationToken);

        var nutritionist = await linkReader.GetAsync(actorUserId, cancellationToken);
        if (profile is null)
        {
            return Result<NutritionProfileResponse>.Success(new NutritionProfileResponse(
                null, null, null, null, false, null, DateTime.UtcNow,
                LinkedToNutritionist: nutritionist is not null, Nutritionist: nutritionist));
        }

        return Result<NutritionProfileResponse>.Success(NutritionProfileMapper.ToResponse(profile, nutritionist));
    }
}
