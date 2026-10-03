using Moq;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Clients.AcceptInvite;
using ShapeUp.Features.Nutrition.Clients.EndRelationship;
using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;
using ShapeUp.Shared.Results;

namespace UnitTests.Domains.Nutrition.Clients;

public class NutritionClientLinkHandlersTests
{
    private readonly Mock<IProfessionalCapabilityService> _capabilities = new();
    private readonly Mock<IProfessionalClientInviteRepository> _invites = new();
    private readonly Mock<IProfessionalClientRelationshipRepository> _relationships = new();

    [Fact]
    public async Task Invite_WithoutNutritionCapability_Returns403()
    {
        _capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(true, false));

        var result = await new InviteNutritionClientHandler(_capabilities.Object, _invites.Object).HandleAsync(1, default);

        Assert.Equal(403, result.Error!.StatusCode);
        _invites.Verify(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Invite_StoresOnlyTheHashOfTheToken()
    {
        _capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        ProfessionalClientInvite? stored = null;
        _invites.Setup(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), default)).Callback<ProfessionalClientInvite, CancellationToken>((i, _) => stored = i);

        var result = await new InviteNutritionClientHandler(_capabilities.Object, _invites.Object).HandleAsync(1, default);

        Assert.True(result.IsSuccess);
        Assert.NotNull(stored);
        Assert.Equal("Nutrition", stored!.RelationshipType);
        Assert.NotEqual(result.Value!.Token, stored.TokenHash);
        Assert.Equal(InviteNutritionClientHandler.ComputeHash(result.Value.Token), stored.TokenHash);
    }

    private ProfessionalClientInvite PendingInvite(string token, DateTime? expires = null) => new()
    {
        ProfessionalUserId = 1,
        RelationshipType = "Nutrition",
        TokenHash = InviteNutritionClientHandler.ComputeHash(token),
        ExpiresAtUtc = expires ?? DateTime.UtcNow.AddDays(1)
    };

    private AcceptNutritionInviteHandler Accept() => new(_invites.Object, _relationships.Object);

    [Fact]
    public async Task Accept_ValidInvite_CreatesNutritionRelationshipAndConsumesInvite()
    {
        var invite = PendingInvite("tok");
        _invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);
        _relationships.Setup(r => r.CreateAsync(It.IsAny<ProfessionalClientRelationship>(), default))
            .ReturnsAsync((ProfessionalClientRelationship r, CancellationToken _) => Result<ProfessionalClientRelationship>.Success(r));

        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 2, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.NutritionistUserId);
        _relationships.Verify(r => r.CreateAsync(It.Is<ProfessionalClientRelationship>(x =>
            x.ProfessionalUserId == 1 && x.ClientUserId == 2 && x.RelationshipType == "Nutrition"), default), Times.Once);
        Assert.Equal(ProfessionalClientInviteStatus.Accepted, invite.Status);
        Assert.Equal(2, invite.AcceptedByUserId);
    }

    [Fact]
    public async Task Accept_UnknownToken_Returns404()
    {
        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("nope"), 2, default);

        Assert.Equal(404, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Accept_ExpiredInvite_FailsWithoutCreatingRelationship()
    {
        var invite = PendingInvite("tok", DateTime.UtcNow.AddMinutes(-1));
        _invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);

        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 2, default);

        Assert.True(result.IsFailure);
        _relationships.Verify(r => r.CreateAsync(It.IsAny<ProfessionalClientRelationship>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Accept_AlreadyAcceptedInvite_Returns409()
    {
        var invite = PendingInvite("tok");
        invite.Status = ProfessionalClientInviteStatus.Accepted;
        _invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);

        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 2, default);

        Assert.Equal(409, result.Error!.StatusCode);
    }

    [Fact]
    public async Task Accept_OwnInvite_IsRejected()
    {
        var invite = PendingInvite("tok");
        _invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);

        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 1, default);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task End_ByThirdParty_Returns403()
    {
        var result = await new EndNutritionRelationshipHandler(_relationships.Object).HandleAsync(3, 1, 2, default);

        Assert.Equal(403, result.Error!.StatusCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task End_ByEitherSide_EndsTheRelationship(int actor)
    {
        _relationships.Setup(r => r.EndActiveAsync(1, 2, "Nutrition", It.IsAny<DateTime>(), default)).ReturnsAsync(true);

        var result = await new EndNutritionRelationshipHandler(_relationships.Object).HandleAsync(actor, 1, 2, default);

        Assert.True(result.IsSuccess);
    }
}
