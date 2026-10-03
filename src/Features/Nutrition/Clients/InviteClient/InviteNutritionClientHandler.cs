using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Policies;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Results;
using System.Security.Cryptography;
using System.Text;

namespace ShapeUp.Features.Nutrition.Clients.InviteClient;

/// <summary>
/// The nutritionist generates a single-use token and hands it to the client (link, QR, message).
/// Nothing is shared until the client accepts it.
/// </summary>
public class InviteNutritionClientHandler(
    IProfessionalCapabilityService capabilityService,
    IProfessionalClientInviteRepository inviteRepository)
{
    public static readonly TimeSpan InviteLifetime = TimeSpan.FromDays(7);

    public async Task<Result<InviteNutritionClientResponse>> HandleAsync(int nutritionistUserId, CancellationToken cancellationToken)
    {
        var capabilities = await capabilityService.GetAsync(nutritionistUserId, cancellationToken);
        if (!capabilities.Nutrition)
            return Result<InviteNutritionClientResponse>.Failure(CommonErrors.Forbidden("Nutrition capability is required."));

        var token = GenerateToken();
        var nowUtc = DateTime.UtcNow;
        await inviteRepository.AddAsync(new ProfessionalClientInvite
        {
            ProfessionalUserId = nutritionistUserId,
            RelationshipType = NutritionAccessPolicy.RelationshipType,
            TokenHash = ComputeHash(token),
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(InviteLifetime)
        }, cancellationToken);

        return Result<InviteNutritionClientResponse>.Success(new InviteNutritionClientResponse(token, nowUtc.Add(InviteLifetime)));
    }

    internal static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public static string ComputeHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
