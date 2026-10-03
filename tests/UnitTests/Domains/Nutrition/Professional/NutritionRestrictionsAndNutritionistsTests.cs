using ShapeUp.Features.Nutrition.Clients.ListNutritionists;
using ShapeUp.Features.Nutrition.Clients.Shared;
using ShapeUp.Features.Nutrition.Profile.SetDietaryRestrictions;
using ShapeUp.Features.Nutrition.Profile.Shared.ViewModels;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Abstractions;
using ShapeUp.Features.Relationships.Shared.Entities;

namespace UnitTests.Domains.Nutrition.Professional;

public class NutritionRestrictionsAndNutritionistsTests
{
    [Fact]
    public async Task SetRestrictions_CreatesProfileTrimsAndClearsBlank()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var handler = new SetDietaryRestrictionsHandler(db, new SetDietaryRestrictionsCommandValidator());

        var first = await handler.HandleAsync(new SetDietaryRestrictionsCommand(" lactose free ", "peanut"), 5, default);
        var second = await handler.HandleAsync(new SetDietaryRestrictionsCommand("  ", "peanut"), 5, default);

        Assert.Equal("lactose free", first.Value!.Restrictions);
        Assert.Equal("peanut", first.Value.Allergies);
        Assert.Null(second.Value!.Restrictions);
        Assert.Single(db.Profiles);
    }

    [Fact]
    public async Task SetRestrictions_TooLong_Returns400()
    {
        var handler = new SetDietaryRestrictionsHandler(NutritionProfessionalTestSupport.NewDb(), new SetDietaryRestrictionsCommandValidator());

        var result = await handler.HandleAsync(new SetDietaryRestrictionsCommand(new string('a', 1001), null), 5, default);

        Assert.Equal(400, result.Error!.StatusCode);
    }

    [Fact]
    public async Task SetRestrictions_ThroughClientAccess_ProfessionalWithoutLinkGets403()
    {
        var db = NutritionProfessionalTestSupport.NewDb();
        var handler = new SetDietaryRestrictionsHandler(db, new SetDietaryRestrictionsCommandValidator());
        var access = new NutritionClientAccess(NutritionProfessionalTestSupport.Policy((1, 2)).Object);
        var command = new SetDietaryRestrictionsCommand("vegan", null);

        var linked = await access.RunAsync(1, 2, id => handler.HandleAsync(command, id, default), default);
        var stranger = await access.RunAsync(9, 2, id => handler.HandleAsync(command, id, default), default);

        Assert.Equal(2, db.Profiles.Single().UserId);
        Assert.True(linked.IsSuccess);
        Assert.Equal(403, stranger.Error!.StatusCode);
    }

    [Fact]
    public async Task ListMyNutritionists_ReturnsActiveLinksWithNameAndStart()
    {
        var started = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var relationships = new Mock<IProfessionalClientRelationshipRepository>();
        relationships.Setup(r => r.ListActiveByClientAsync(2, "Nutrition", default))
            .ReturnsAsync([new ProfessionalClientRelationship
            {
                ProfessionalUserId = 1, ClientUserId = 2, RelationshipType = "Nutrition", StartedAt = started
            }]);
        var handler = new ListMyNutritionistsHandler(relationships.Object, NutritionProfessionalTestSupport.Users((1, "Dr. Pro")).Object);

        var result = await handler.HandleAsync(2, default);

        var item = Assert.Single(result.Value!);
        Assert.Equal(new NutritionistResponse(1, "Dr. Pro", started), item);
    }
}
