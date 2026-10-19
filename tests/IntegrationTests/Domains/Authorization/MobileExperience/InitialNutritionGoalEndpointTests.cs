using System.Net.Http.Json;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Profile;
using ShapeUp.Features.Nutrition.Profile.CompleteOnboarding;
using ShapeUp.Features.Nutrition.Shared.Abstractions;
using ShapeUp.Features.Nutrition.Shared.Documents;
using ShapeUp.Features.Nutrition.Shared.Entities;
using ShapeUp.Features.Nutrition.Shared.ValueObjects;

namespace IntegrationTests.Domains.Authorization.MobileExperience;

public class InitialNutritionGoalEndpointTests
{
    [Theory]
    [InlineData(180, 7, true, false, 200)]
    [InlineData(99, 7, true, false, 400)]
    [InlineData(180, 7, false, false, 401)]
    [InlineData(180, 8, true, false, 403)]
    [InlineData(180, 7, true, true, 409)]
    public async Task InitialNutritionGoalContract(int height, int expectedUserId, bool authenticated, bool existingGoal, int expectedStatus)
    {
        var database = Guid.NewGuid().ToString();
        var weights = new Mock<IWeightTrackingRepository>();
        weights.Setup(w => w.GetRegistersByRangeAsync(7, It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WeightRegisterDocument> { new() { UserId = 7, Day = "2026-10-18", Weight = 80 } });
        using var server = new TestServer(new WebHostBuilder().ConfigureServices(services =>
        {
            services.AddLogging();
            services.AddControllers().AddApplicationPart(typeof(NutritionProfileController).Assembly);
            services.AddDbContext<NutritionDbContext>(o => o.UseInMemoryDatabase(database));
            services.AddSingleton(weights.Object);
            services.AddScoped<CompleteOnboardingHandler>();
            services.AddScoped<IValidator<CompleteOnboardingCommand>, CompleteOnboardingCommandValidator>();
        }).Configure(app =>
        {
            app.UseRouting();
            // Test identity boundary; production uses the Firebase AuthorizationMiddleware.
            app.Use(async (context, next) =>
            {
                if (!authenticated) { context.Response.StatusCode = 401; return; }
                context.Items["UserId"] = 7;
                await next();
            });
            app.UseEndpoints(endpoints => endpoints.MapControllers());
        }));
        if (existingGoal)
        {
            using var scope = server.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<NutritionDbContext>();
            db.Profiles.Add(new NutritionProfile { UserId = 7, ActiveGoal = new MacroValueObject { Kcal = 2200 } });
            await db.SaveChangesAsync();
        }
        using var client = server.CreateClient();
        var response = await client.PostAsJsonAsync("/api/nutrition/profile/onboarding",
            new { heightCm = height, age = 30, biologicalSex = "Male", activityLevel = "Moderate", onlyIfMissing = true, expectedUserId });
        Assert.Equal(expectedStatus, (int)response.StatusCode);
    }
}
