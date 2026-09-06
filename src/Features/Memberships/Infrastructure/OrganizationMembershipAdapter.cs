namespace ShapeUp.Features.Memberships.Infrastructure;

using Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using GymStaffRole = ShapeUp.Features.GymManagement.Shared.Entities.GymStaffRole;

public class OrganizationMembershipAdapter(
    IGymRepository gymRepository,
    IGymStaffRepository gymStaffRepository) : IOrganizationMembershipRepository
{
    public async Task<OrganizationMembership?> GetMembershipAsync(int userId, int gymId, CancellationToken cancellationToken)
    {
        var gym = await gymRepository.GetByIdAsync(gymId, cancellationToken);
        if (gym is null)
            return null;

        if (gym.OwnerId == userId)
            return new OrganizationMembership(userId, gymId, MembershipRole.Owner);

        var staff = await gymStaffRepository.GetByGymAndUserAsync(gymId, userId, cancellationToken);
        if (staff is null || !staff.IsActive)
            return null;

        return new OrganizationMembership(userId, gymId, MapRole(staff.Role));
    }

    private static MembershipRole MapRole(GymStaffRole role) => role switch
    {
        GymStaffRole.Trainer => MembershipRole.Trainer,
        GymStaffRole.Receptionist => MembershipRole.Receptionist,
        GymStaffRole.Manager => MembershipRole.Manager,
        GymStaffRole.Finance => MembershipRole.Finance,
        GymStaffRole.Staff => MembershipRole.Staff,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unmapped GymStaffRole")
    };
}
