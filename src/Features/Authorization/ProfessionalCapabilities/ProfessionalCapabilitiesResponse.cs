namespace ShapeUp.Features.Authorization.ProfessionalCapabilities;

/// <summary>Which professional work the caller may do; the apps use it to show or hide the coach tabs.</summary>
public record ProfessionalCapabilitiesResponse(bool Training, bool Nutrition);
