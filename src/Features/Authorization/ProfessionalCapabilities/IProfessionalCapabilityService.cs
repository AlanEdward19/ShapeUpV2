namespace ShapeUp.Features.Authorization.ProfessionalCapabilities;

/// <summary>Single source of truth for which professional work a user may do (training, nutrition).</summary>
public interface IProfessionalCapabilityService
{
    Task<ProfessionalCapabilitiesResponse> GetAsync(int userId, CancellationToken cancellationToken);
}
