using ShapeUp.Features.Nutrition.Shared;
using ShapeUp.Features.Nutrition.Shared.Entities;

namespace UnitTests.Domains.Nutrition;

public class TdeeCalculatorTests
{
    [Fact]
    public void Calculate_ForMaleModerateProfile_ReturnsExpectedMacroGoal()
    {
        var profile = new NutritionProfile
        {
            HeightCm = 180,
            Age = 30,
            BiologicalSex = "Male",
            ActivityLevel = "Moderate"
        };

        var goal = TdeeCalculator.Calculate(profile, 80m);

        Assert.Equal(2759, goal.Kcal);
        Assert.Equal(207, goal.ProteinG);
        Assert.Equal(276, goal.CarbG);
        Assert.Equal(92, goal.FatG);
    }

    [Fact]
    public void Calculate_ForFemaleLightProfile_ReturnsExpectedMacroGoal()
    {
        var profile = new NutritionProfile
        {
            HeightCm = 165,
            Age = 28,
            BiologicalSex = "Female",
            ActivityLevel = "Light"
        };

        var goal = TdeeCalculator.Calculate(profile, 65m);

        Assert.Equal(1898, goal.Kcal);
        Assert.Equal(142, goal.ProteinG);
        Assert.Equal(190, goal.CarbG);
        Assert.Equal(63, goal.FatG);
    }

    [Fact]
    public void Calculate_WhenProfileIsIncomplete_ThrowsInvalidOperationException()
    {
        var profile = new NutritionProfile
        {
            HeightCm = null,
            Age = 30,
            BiologicalSex = "Male",
            ActivityLevel = "Moderate"
        };

        Assert.Throws<InvalidOperationException>(() => TdeeCalculator.Calculate(profile, 80m));
    }
}
