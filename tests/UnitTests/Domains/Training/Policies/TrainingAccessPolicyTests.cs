using Microsoft.EntityFrameworkCore;
using Moq;
using ShapeUp.Features.GymManagement.Infrastructure.Data;
using ShapeUp.Features.GymManagement.Shared.Entities;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Features.Training.Infrastructure.Policies;

namespace UnitTests.Domains.Training.Policies;

public class TrainingAccessPolicyTests
{
    private readonly GymManagementDbContext _gymContext;
    private readonly Mock<IProfessionalClientRelationshipRepository> _relationshipRepository = new();
    private readonly TrainingAccessPolicy _policy;

    public TrainingAccessPolicyTests()
    {
        var options = new DbContextOptionsBuilder<GymManagementDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _gymContext = new GymManagementDbContext(options);
        _policy = new TrainingAccessPolicy(_gymContext, _relationshipRepository.Object);
    }

    [Fact]
    public async Task CanCreateWorkoutForAsync_ActorIsTarget_ReturnsTrue()
    {
        var result = await _policy.CanCreateWorkoutForAsync(1, 1, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CanCreateWorkoutForAsync_ActiveProfessionalRelationshipExists_ReturnsTrue()
    {
        _relationshipRepository.Setup(r => r.GetActiveAsync(1, 2, "Training", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalClientRelationship { ProfessionalUserId = 1, ClientUserId = 2, RelationshipType = "Training", StartedAt = DateTime.UtcNow });

        var result = await _policy.CanCreateWorkoutForAsync(1, 2, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CanCreateWorkoutForAsync_DirectTrainerClientLink_ReturnsTrue()
    {
        _gymContext.TrainerClients.Add(new TrainerClient { TrainerId = 1, ClientId = 2, IsActive = true, TrainerPlanId = 5 });
        await _gymContext.SaveChangesAsync();

        var result = await _policy.CanCreateWorkoutForAsync(1, 2, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CanCreateWorkoutForAsync_TrainerClientLinkWithoutPlan_ReturnsFalseForThatPath()
    {
        _gymContext.TrainerClients.Add(new TrainerClient { TrainerId = 1, ClientId = 2, IsActive = true, TrainerPlanId = null });
        await _gymContext.SaveChangesAsync();

        var result = await _policy.CanCreateWorkoutForAsync(1, 2, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task CanCreateWorkoutForAsync_SameGymTrainerAndClient_ReturnsTrue()
    {
        _gymContext.GymStaff.Add(new GymStaff { GymId = 100, UserId = 1, Role = GymStaffRole.Trainer, IsActive = true });
        _gymContext.GymClients.Add(new GymClient { GymId = 100, UserId = 2, GymPlanId = 1, IsActive = true });
        await _gymContext.SaveChangesAsync();

        var result = await _policy.CanCreateWorkoutForAsync(1, 2, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task CanCreateWorkoutForAsync_NoRelationAtAll_ReturnsFalse()
    {
        var result = await _policy.CanCreateWorkoutForAsync(1, 2, CancellationToken.None);

        Assert.False(result);
    }
}
