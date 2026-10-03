using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Authorization.Shared.Abstractions;
using ShapeUp.Features.Authorization.Shared.Entities;
using ShapeUp.Features.Nutrition.Infrastructure.Data;
using ShapeUp.Features.Nutrition.Shared.Abstractions;

namespace UnitTests.Domains.Nutrition.Professional;

internal static class NutritionProfessionalTestSupport
{
    public static NutritionDbContext NewDb() =>
        new(new DbContextOptionsBuilder<NutritionDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    /// <summary>Policy that allows the pairs given (and always self).</summary>
    public static Mock<INutritionAccessPolicy> Policy(params (int Actor, int Target)[] allowed)
    {
        var policy = new Mock<INutritionAccessPolicy>();
        policy.Setup(p => p.CanManageNutritionForAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int actor, int target, CancellationToken _) =>
                actor == target || allowed.Contains((actor, target)));
        return policy;
    }

    public static Mock<IUserRepository> Users(params (int Id, string Name)[] users)
    {
        var repository = new Mock<IUserRepository>();
        repository.Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) =>
                users.Where(u => u.Id == id)
                    .Select(u => new User { Id = u.Id, FirebaseUid = $"uid-{u.Id}", Email = $"{u.Id}@test", DisplayName = u.Name })
                    .FirstOrDefault());
        return repository;
    }
}
