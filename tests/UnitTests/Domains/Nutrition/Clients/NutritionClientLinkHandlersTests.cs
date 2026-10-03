using Moq;
using ShapeUp.Features.Authorization.ProfessionalCapabilities;
using ShapeUp.Features.Nutrition.Clients.AcceptInvite;
using ShapeUp.Features.Nutrition.Clients.EndRelationship;
using ShapeUp.Features.Nutrition.Clients.InviteClient;
using ShapeUp.Features.Nutrition.Clients.ListInvites;
using ShapeUp.Features.Nutrition.Clients.RevokeInvite;
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
        _invites.Setup(i => i.TryAcceptAsync(invite.Id, 2, It.IsAny<DateTime>(), default)).ReturnsAsync(true);
        _relationships.Setup(r => r.CreateAsync(It.IsAny<ProfessionalClientRelationship>(), default))
            .ReturnsAsync((ProfessionalClientRelationship r, CancellationToken _) => Result<ProfessionalClientRelationship>.Success(r));

        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 2, default);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.NutritionistUserId);
        _relationships.Verify(r => r.CreateAsync(It.Is<ProfessionalClientRelationship>(x =>
            x.ProfessionalUserId == 1 && x.ClientUserId == 2 && x.RelationshipType == "Nutrition"), default), Times.Once);
        _invites.Verify(i => i.TryAcceptAsync(invite.Id, 2, It.IsAny<DateTime>(), default), Times.Once);
    }

    [Fact]
    public async Task Accept_LosesTheRaceForTheSingleUseInvite_Returns409AndCreatesNothing()
    {
        var invite = PendingInvite("tok");
        _invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);
        _invites.Setup(i => i.TryAcceptAsync(invite.Id, 3, It.IsAny<DateTime>(), default)).ReturnsAsync(false);

        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 3, default);

        Assert.Equal(409, result.Error!.StatusCode);
        _relationships.Verify(r => r.CreateAsync(It.IsAny<ProfessionalClientRelationship>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Accept_RelationshipCreationFails_GivesTheInviteBack()
    {
        var invite = PendingInvite("tok");
        _invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);
        _invites.Setup(i => i.TryAcceptAsync(invite.Id, 2, It.IsAny<DateTime>(), default)).ReturnsAsync(true);
        _relationships.Setup(r => r.CreateAsync(It.IsAny<ProfessionalClientRelationship>(), default))
            .ReturnsAsync(Result<ProfessionalClientRelationship>.Failure(CommonErrors.Conflict("x")));

        var result = await Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 2, default);

        Assert.True(result.IsFailure);
        _invites.Verify(i => i.ReleaseAcceptedAsync(invite.Id, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Accept_RelationshipCreationThrows_GivesTheInviteBackAndRethrows()
    {
        var invite = PendingInvite("tok");
        _invites.Setup(i => i.GetByTokenHashAsync(invite.TokenHash, default)).ReturnsAsync(invite);
        _invites.Setup(i => i.TryAcceptAsync(invite.Id, 2, It.IsAny<DateTime>(), default)).ReturnsAsync(true);
        _relationships.Setup(r => r.CreateAsync(It.IsAny<ProfessionalClientRelationship>(), default)).ThrowsAsync(new InvalidOperationException("db"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => Accept().HandleAsync(new AcceptNutritionInviteCommand("tok"), 2, default));

        _invites.Verify(i => i.ReleaseAcceptedAsync(invite.Id, 2, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Invite_AtThePendingLimit_Returns409()
    {
        _capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        _invites.Setup(i => i.CountPendingAsync(1, "Nutrition", It.IsAny<DateTime>(), default))
            .ReturnsAsync(InviteNutritionClientHandler.MaxPendingInvites);

        var result = await new InviteNutritionClientHandler(_capabilities.Object, _invites.Object).HandleAsync(1, default);

        Assert.Equal(409, result.Error!.StatusCode);
        _invites.Verify(i => i.AddAsync(It.IsAny<ProfessionalClientInvite>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_OwnPendingInvite_Succeeds()
    {
        var invite = PendingInvite("tok");
        invite.Id = 5;
        _invites.Setup(i => i.GetByIdAsync(5, default)).ReturnsAsync(invite);
        _invites.Setup(i => i.TryRevokeAsync(5, 1, default)).ReturnsAsync(true);

        var result = await new RevokeNutritionInviteHandler(_invites.Object).HandleAsync(5, 1, default);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Revoke_InviteOfAnotherNutritionistOrMissing_Returns404()
    {
        var invite = PendingInvite("tok");
        invite.Id = 5;
        _invites.Setup(i => i.GetByIdAsync(5, default)).ReturnsAsync(invite);

        Assert.Equal(404, (await new RevokeNutritionInviteHandler(_invites.Object).HandleAsync(5, 99, default)).Error!.StatusCode);
        Assert.Equal(404, (await new RevokeNutritionInviteHandler(_invites.Object).HandleAsync(6, 1, default)).Error!.StatusCode);
        _invites.Verify(i => i.TryRevokeAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_AlreadyAccepted_Returns409()
    {
        var invite = PendingInvite("tok");
        invite.Id = 5;
        invite.Status = ProfessionalClientInviteStatus.Accepted;
        _invites.Setup(i => i.GetByIdAsync(5, default)).ReturnsAsync(invite);

        Assert.Equal(409, (await new RevokeNutritionInviteHandler(_invites.Object).HandleAsync(5, 1, default)).Error!.StatusCode);
    }

    [Fact]
    public async Task ListInvites_ReturnsPageWithCursor_AndRequiresCapability()
    {
        _capabilities.Setup(c => c.GetAsync(1, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(false, true));
        _capabilities.Setup(c => c.GetAsync(2, default)).ReturnsAsync(new ProfessionalCapabilitiesResponse(true, false));
        _invites.Setup(i => i.ListPendingAsync(1, "Nutrition", It.IsAny<DateTime>(), null, 3, default))
            .ReturnsAsync([new ProfessionalClientInvite { Id = 1, RelationshipType = "Nutrition", TokenHash = "a" },
                           new ProfessionalClientInvite { Id = 2, RelationshipType = "Nutrition", TokenHash = "b" },
                           new ProfessionalClientInvite { Id = 3, RelationshipType = "Nutrition", TokenHash = "c" }]);
        var handler = new ListNutritionInvitesHandler(_capabilities.Object, _invites.Object);

        var page = await handler.HandleAsync(new ListNutritionInvitesQuery(null, 2), 1, default);
        var forbidden = await handler.HandleAsync(new ListNutritionInvitesQuery(null, 2), 2, default);
        var bad = await handler.HandleAsync(new ListNutritionInvitesQuery("???", 2), 1, default);

        Assert.Equal([1, 2], page.Value!.Items.Select(i => i.InviteId).ToArray());
        Assert.NotNull(page.Value.NextCursor);
        Assert.Equal(403, forbidden.Error!.StatusCode);
        Assert.Equal(400, bad.Error!.StatusCode);
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
        _invites.Verify(i => i.TryAcceptAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);

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
