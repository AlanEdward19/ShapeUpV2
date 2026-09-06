namespace ShapeUp.Features.Memberships.Shared.Abstractions;

public record OrganizationMembership(int UserId, int GymId, MembershipRole Role);
