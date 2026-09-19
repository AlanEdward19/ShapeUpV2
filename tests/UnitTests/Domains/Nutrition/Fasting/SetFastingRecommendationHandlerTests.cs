using Moq;
using ShapeUp.Features.Nutrition.Fasting.SetRecommendation;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Fasting;

public sealed class SetFastingRecommendationHandlerTests
{
    private const int ProfessionalUserId = 11;
    private const int ClientUserId = 22;

    [Fact]
    public async Task HandleAsync_WithActiveTrainingRelationship_StoresRecommendationWithoutOverride()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var clock = new FixedUtcClock(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        var relationshipRepository = new Mock<IProfessionalClientRelationshipRepository>();
        relationshipRepository
            .Setup(x => x.GetActiveAsync(ProfessionalUserId, ClientUserId, "Training", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProfessionalClientRelationship
            {
                ProfessionalUserId = ProfessionalUserId,
                ClientUserId = ClientUserId,
                RelationshipType = "Training",
                StartedAt = clock.UtcNow
            });

        var handler = new SetFastingRecommendationHandler(
            db,
            clock,
            relationshipRepository.Object,
            new SetFastingRecommendationCommandValidator());

        var result = await handler.HandleAsync(
            new SetFastingRecommendationCommand("18:6"),
            ProfessionalUserId,
            ClientUserId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("18:6", result.Value!.Protocol);
        Assert.Equal(18, result.Value.FastHours);

        var stored = db.FastingAgendas.Single(a => a.UserId == ClientUserId);
        Assert.Equal("18:6", stored.RecommendedProtocol);
        Assert.Equal(18, stored.RecommendedFastHours);
        Assert.Null(stored.FastHours);
        Assert.Empty(db.FastingOverrides);
    }

    [Fact]
    public async Task HandleAsync_WithoutRelationship_Returns403()
    {
        await using var db = FastingTestSupport.CreateDbContext();
        var clock = new FixedUtcClock(new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc));
        var relationshipRepository = new Mock<IProfessionalClientRelationshipRepository>();
        relationshipRepository
            .Setup(x => x.GetActiveAsync(ProfessionalUserId, ClientUserId, "Training", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProfessionalClientRelationship?)null);

        var handler = new SetFastingRecommendationHandler(
            db,
            clock,
            relationshipRepository.Object,
            new SetFastingRecommendationCommandValidator());

        var result = await handler.HandleAsync(
            new SetFastingRecommendationCommand("16:8"),
            ProfessionalUserId,
            ClientUserId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Empty(db.FastingAgendas);
    }
}
