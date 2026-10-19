namespace ShapeUp.Features.Authorization.UserManagement.MobileExperience;

public record RegisterMobileExperienceCommand(string Platform);
public record MobileExperienceResponse(bool FirstMobileVisit, DateTime FirstSeenAtUtc);
