using ShapeUp.Features.Credentials.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.ProfessionalCapabilities;

/// <summary>
/// Training: active Trainer platform role (the way trainers are provisioned today) or a verified
/// "PersonalTrainer" credential. Nutrition: only a verified "Nutritionist" credential -- there is no
/// nutritionist platform role.
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

        var nutrition = await credentialRepository.GetVerifiedAsync(userId, NutritionistProfession, nowUtc, cancellationToken) is not null;

        return Result<ProfessionalCapabilitiesResponse>.Success(new ProfessionalCapabilitiesResponse(training, nutrition));
    }
}
