using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Nutrition.Fasting.Shared;
using ShapeUp.Features.Nutrition.Infrastructure.Data;

namespace UnitTests.Domains.Nutrition.Fasting;

internal sealed class FixedUtcClock(DateTime utcNow) : IUtcClock
{
    public DateTime UtcNow { get; } = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
}

internal static class FastingTestSupport
{
    internal const int UserId = 7;
    internal const string SaoPaulo = "America/Sao_Paulo";

    internal static NutritionDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NutritionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NutritionDbContext(options);
    }
}
