using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace UnitTests.Domains.Nutrition.Profile;
public class InitialNutritionGoalTests
{
    private static NutritionDbContext Database() => new(new DbContextOptionsBuilder<NutritionDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static Mock<IWeightTrackingRepository> Weights()
    {
        var weights = new Mock<IWeightTrackingRepository>();
        weights.Setup(w => w.GetRegistersByRangeAsync(7, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeightRegisterDocument> { new() { UserId = 7, Day = "2026-10-18", Weight = 80 } });
        return weights;
    }
    private static INutritionistLinkReader NoLink() => Mock.Of<INutritionistLinkReader>();
    [Fact]
    public async Task ConfirmedCalculationPersistsProfileAndGoal()
    {
        await using var db = Database();
        var handler = new CompleteOnboardingHandler(db, Weights().Object, NoLink(), new CompleteOnboardingCommandValidator());
        var result = await handler.HandleAsync(new(180, 30, "Male", "Moderate", true, 7), 7, CancellationToken.None);
        Assert.True(result.IsSuccess);
        db.ChangeTracker.Clear();
        var stored = await db.Profiles.Include(p => p.ActiveGoal).SingleAsync();
        Assert.Equal(2759, stored.ActiveGoal!.Kcal);
        Assert.Equal(180, stored.HeightCm);
        Assert.Equal(30, stored.Age);
        Assert.Equal("Male", stored.BiologicalSex);
        Assert.Equal("Moderate", stored.ActivityLevel);
    }
    [Fact]
    public async Task ExistingGoalSurvivesReplay()
    {
        await using var db = Database();
        db.Profiles.Add(new NutritionProfile { UserId = 7, ActiveGoal = new MacroValueObject { Kcal = 2200, ProteinG = 150, CarbG = 250, FatG = 67 } });
        await db.SaveChangesAsync();
        var result = await new CompleteOnboardingHandler(db, Weights().Object, NoLink(), new CompleteOnboardingCommandValidator())
            .HandleAsync(new(180, 30, "Male", "Moderate", true, 7), 7, CancellationToken.None);
        Assert.Equal(409, result.Error!.StatusCode);
        Assert.Equal(2200, (await db.Profiles.SingleAsync()).ActiveGoal!.Kcal);
    }
    [Fact]
    public async Task QueueFromAnotherAccountIsRejectedBeforeAnyWrite()
    {
        await using var db = Database();
        var weights = Weights();
        var result = await new CompleteOnboardingHandler(db, weights.Object, NoLink(), new CompleteOnboardingCommandValidator())
            .HandleAsync(new(180, 30, "Male", "Moderate", true, 8), 7, CancellationToken.None);
        Assert.Equal(403, result.Error!.StatusCode);
        Assert.Empty(await db.Profiles.ToListAsync());
        weights.VerifyNoOtherCalls();
    }
}

public class NutritionProfileLinkTests
{
    private static NutritionDbContext Database() => new(new DbContextOptionsBuilder<NutritionDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task Get_WithoutProfileButLinked_ReturnsLinkInfo()
    {
        await using var db = Database();
        var reader = new Mock<INutritionistLinkReader>();
        reader.Setup(r => r.GetAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ShapeUp.Features.Nutrition.Profile.Shared.ViewModels.NutritionistLinkDto(9, "Dra. Ana"));

        var result = await new ShapeUp.Features.Nutrition.Profile.GetNutritionProfile.GetNutritionProfileHandler(db, reader.Object)
            .HandleAsync(new(), 7, default);

        Assert.True(result.Value!.LinkedToNutritionist);
        Assert.Equal(9, result.Value.Nutritionist!.UserId);
        Assert.Equal("Dra. Ana", result.Value.Nutritionist.DisplayName);
    }

    [Fact]
    public async Task Get_WithProfileAndNoLink_IsNotLinked()
    {
        await using var db = Database();
        db.Profiles.Add(new NutritionProfile { UserId = 7 });
        await db.SaveChangesAsync();

        var result = await new ShapeUp.Features.Nutrition.Profile.GetNutritionProfile.GetNutritionProfileHandler(db, Mock.Of<INutritionistLinkReader>())
            .HandleAsync(new(), 7, default);

        Assert.False(result.Value!.LinkedToNutritionist);
        Assert.Null(result.Value.Nutritionist);
    }
}
