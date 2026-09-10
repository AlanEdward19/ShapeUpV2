using ShapeUp.Features.Nutrition.Foods.Shared;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Foods;

public class FoodVersionResolverTests
{
    [Fact]
    public void Resolve_WhenActiveOverrideExists_ReturnsOverrideMacrosWithPersonalFlag()
    {
        var publicFood = new FoodDocument
        {
            Id = "food-1",
            Name = "Public Rice",
            MacrosPer100 = new MacroValueObject { Kcal = 100, ProteinG = 2, CarbG = 20, FatG = 1 },
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        var overrideDocument = new FoodOverrideDocument
        {
            Id = "override-1",
            FoodId = publicFood.Id,
            UserId = 42,
            MacrosPer100 = new MacroValueObject { Kcal = 150, ProteinG = 5, CarbG = 25, FatG = 3 },
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = FoodVersionResolver.Resolve(publicFood, overrideDocument);

        Assert.Equal(publicFood.Id, result.Id);
        Assert.True(result.IsPersonalOverride);
        Assert.Equal("override-1", result.OverrideId);
        Assert.Equal(150, result.MacrosPer100.Kcal);
        Assert.Equal(5, result.MacrosPer100.ProteinG);
    }

    [Fact]
    public void Resolve_WhenNoActiveOverride_ReturnsPublicMacrosWithoutPersonalFlag()
    {
        var publicFood = new FoodDocument
        {
            Id = "food-2",
            Name = "Public Beans",
            MacrosPer100 = new MacroValueObject { Kcal = 90, ProteinG = 6, CarbG = 15, FatG = 1 },
            CreatedByUserId = 2,
            CreatedAtUtc = DateTime.UtcNow
        };

        var inactiveOverride = new FoodOverrideDocument
        {
            Id = "override-2",
            FoodId = publicFood.Id,
            UserId = 42,
            MacrosPer100 = new MacroValueObject { Kcal = 120, ProteinG = 8, CarbG = 18, FatG = 2 },
            IsActive = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        var result = FoodVersionResolver.Resolve(publicFood, inactiveOverride);

        Assert.False(result.IsPersonalOverride);
        Assert.Null(result.OverrideId);
        Assert.Equal(90, result.MacrosPer100.Kcal);
        Assert.Equal(6, result.MacrosPer100.ProteinG);
    }

    [Fact]
    public void Resolve_WhenDifferentUsersHaveOverrides_UsesOnlyActiveOverridePassedIn()
    {
        var publicFood = new FoodDocument
        {
            Id = "food-3",
            Name = "Shared Food",
            MacrosPer100 = new MacroValueObject { Kcal = 200, ProteinG = 10, CarbG = 30, FatG = 5 },
            CreatedByUserId = 1,
            CreatedAtUtc = DateTime.UtcNow
        };

        var userAOverride = new FoodOverrideDocument
        {
            Id = "override-a",
            FoodId = publicFood.Id,
            UserId = 10,
            MacrosPer100 = new MacroValueObject { Kcal = 210, ProteinG = 11, CarbG = 31, FatG = 6 },
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var resolvedForUserA = FoodVersionResolver.Resolve(publicFood, userAOverride);
        var resolvedForUserB = FoodVersionResolver.Resolve(publicFood, null);

        Assert.Equal(210, resolvedForUserA.MacrosPer100.Kcal);
        Assert.True(resolvedForUserA.IsPersonalOverride);
        Assert.Equal(200, resolvedForUserB.MacrosPer100.Kcal);
        Assert.False(resolvedForUserB.IsPersonalOverride);
    }
}
