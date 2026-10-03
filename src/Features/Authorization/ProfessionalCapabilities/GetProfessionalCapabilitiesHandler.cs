using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.ProfessionalCapabilities;

/// <summary>
/// Independent per feature, so one person can be trainer, nutritionist or both.
/// Training: active Trainer platform role or a verified "PersonalTrainer" credential.
/// Nutrition: active Nutritionist platform role or a verified "Nutritionist" credential.
/// </summary>
public class GetProfessionalCapabilitiesHandler(
    IUserPlatformRoleRepository roleRepository,
    IProfessionalCredentialRepository credentialRepository)
{
    public const string PersonalTrainerProfession = "PersonalTrainer";
    public const string NutritionistProfession = "Nutritionist";

    public async Task<Result<ProfessionalCapabilitiesResponse>> HandleAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;

        var trainerRole = await roleRepository.GetByUserIdAndRoleAsync(userId, PlatformRoleType.Trainer, cancellationToken);
        var training = trainerRole is { IsActive: true }
            || await credentialRepository.GetVerifiedAsync(userId, PersonalTrainerProfession, nowUtc, cancellationToken) is not null;

        var nutritionistRole = await roleRepository.GetByUserIdAndRoleAsync(userId, PlatformRoleType.Nutritionist, cancellationToken);
        var nutrition = nutritionistRole is { IsActive: true }
            || await credentialRepository.GetVerifiedAsync(userId, NutritionistProfession, nowUtc, cancellationToken) is not null;

        return Result<ProfessionalCapabilitiesResponse>.Success(new ProfessionalCapabilitiesResponse(training, nutrition));
    }
}
