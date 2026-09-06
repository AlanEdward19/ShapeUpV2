namespace ShapeUp.Features.Memberships.Shared.Abstractions;

public interface IOrganizationMembershipRepository
{
    /// <summary>
    /// Returns the user's membership in the given gym (Owner, or an active GymStaff role),
    /// or null if the user has no membership there (including when the gym itself doesn't exist).
    /// </summary>
    Task<OrganizationMembership?> GetMembershipAsync(int userId, int gymId, CancellationToken cancellationToken);
}
