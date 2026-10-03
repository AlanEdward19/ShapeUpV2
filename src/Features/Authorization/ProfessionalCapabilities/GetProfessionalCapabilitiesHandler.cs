using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Authorization.ProfessionalCapabilities;

public class GetProfessionalCapabilitiesHandler(IProfessionalCapabilityService capabilityService)
{
    public async Task<Result<ProfessionalCapabilitiesResponse>> HandleAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var capabilities = await capabilityService.GetAsync(userId, cancellationToken);
        return Result<ProfessionalCapabilitiesResponse>.Success(capabilities);
    }
}
