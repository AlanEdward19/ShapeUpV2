using Moq;
using ShapeUp.Features.GymManagement.Shared.Abstractions;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Memberships.Infrastructure;
using ShapeUp.Features.Memberships.Shared.Abstractions;

namespace UnitTests.Domains.Memberships;

public class OrganizationMembershipAdapterTests
{
    private readonly Mock<IGymRepository> _gymRepository = new();
    private readonly Mock<IGymStaffRepository> _gymStaffRepository = new();
    private readonly OrganizationMembershipAdapter _adapter;

    public OrganizationMembershipAdapterTests()
    {
        _adapter = new OrganizationMembershipAdapter(_gymRepository.Object, _gymStaffRepository.Object);
    }

    private static Gym BuildGym(int id, int ownerId) => new() { Id = id, OwnerId = ownerId, Name = "Gym" };

    [Fact]
    public async Task GetMembershipAsync_UserIsOwner_ReturnsOwnerMembership()
    {
        _gymRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGym(10, ownerId: 1));

        var result = await _adapter.GetMembershipAsync(1, 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(MembershipRole.Owner, result.Role);
    }

    [Theory]
    [InlineData(GymStaffRole.Trainer, MembershipRole.Trainer)]
    [InlineData(GymStaffRole.Receptionist, MembershipRole.Receptionist)]
    [InlineData(GymStaffRole.Manager, MembershipRole.Manager)]
    [InlineData(GymStaffRole.Finance, MembershipRole.Finance)]
    [InlineData(GymStaffRole.Staff, MembershipRole.Staff)]
    public async Task GetMembershipAsync_ActiveStaff_ReturnsMappedRole(GymStaffRole staffRole, MembershipRole expected)
    {
        _gymRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGym(10, ownerId: 999));
        _gymStaffRepository.Setup(r => r.GetByGymAndUserAsync(10, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GymStaff { GymId = 10, UserId = 2, Role = staffRole, IsActive = true });

        var result = await _adapter.GetMembershipAsync(2, 10, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(expected, result.Role);
    }

    [Fact]
    public async Task GetMembershipAsync_InactiveStaff_ReturnsNull()
    {
        _gymRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGym(10, ownerId: 999));
        _gymStaffRepository.Setup(r => r.GetByGymAndUserAsync(10, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GymStaff { GymId = 10, UserId = 2, Role = GymStaffRole.Trainer, IsActive = false });

        var result = await _adapter.GetMembershipAsync(2, 10, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMembershipAsync_NoMembership_ReturnsNull()
    {
        _gymRepository.Setup(r => r.GetByIdAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildGym(10, ownerId: 999));
        _gymStaffRepository.Setup(r => r.GetByGymAndUserAsync(10, 2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GymStaff?)null);

        var result = await _adapter.GetMembershipAsync(2, 10, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetMembershipAsync_GymDoesNotExist_ReturnsNull()
    {
        _gymRepository.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Gym?)null);

        var result = await _adapter.GetMembershipAsync(1, 999, CancellationToken.None);

        Assert.Null(result);
    }
}
