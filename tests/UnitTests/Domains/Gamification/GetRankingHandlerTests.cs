using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Gamification.GetRanking;
using ShapeUp.Features.Gamification.Infrastructure.Data;
using ShapeUp.Features.Gamification.Shared;
using ShapeUp.Features.Gamification.Shared.Entities;

namespace UnitTests.Domains.Gamification;

public class GetRankingHandlerTests
{
    private readonly GamificationDbContext _dbContext;
    private readonly Mock<IShapeScoreCalculator> _shapeScoreCalculator = new();

    public GetRankingHandlerTests()
    {
        var options = new DbContextOptionsBuilder<GamificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext = new GamificationDbContext(options);
    }

    [Fact]
    public async Task HandleAsync_ReturnsUsersOrderedByShapeScoreDesc()
    {
        SeedProfiles(userIds: [1, 2, 3]);
        SetupScores((1, 40), (2, 90), (3, 70));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new GetRankingQuery(null, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([2, 3, 1], result.Value!.Items.Select(i => i.UserId).ToArray());
        Assert.Equal([90, 70, 40], result.Value.Items.Select(i => i.ShapeScore).ToArray());
        Assert.Null(result.Value.NextCursor);
    }

    [Fact]
    public async Task HandleAsync_WithNextCursor_ReturnsRemainingPageInOrder()
    {
        SeedProfiles(userIds: [10, 20, 30]);
        SetupScores((10, 100), (20, 80), (30, 60));

        var sut = CreateSut();
        var firstPage = await sut.HandleAsync(new GetRankingQuery(null, 2), CancellationToken.None);

        Assert.True(firstPage.IsSuccess);
        Assert.Equal([10, 20], firstPage.Value!.Items.Select(i => i.UserId).ToArray());
        Assert.Equal([100, 80], firstPage.Value.Items.Select(i => i.ShapeScore).ToArray());
        Assert.NotNull(firstPage.Value.NextCursor);

        var secondPage = await sut.HandleAsync(
            new GetRankingQuery(firstPage.Value.NextCursor, 2),
            CancellationToken.None);

        Assert.True(secondPage.IsSuccess);
        Assert.Single(secondPage.Value!.Items);
        Assert.Equal(30, secondPage.Value.Items[0].UserId);
        Assert.Equal(60, secondPage.Value.Items[0].ShapeScore);
        Assert.Null(secondPage.Value.NextCursor);
    }

    [Fact]
    public async Task HandleAsync_WhenShapeScoresTie_OrdersByUserIdAsc()
    {
        SeedProfiles(userIds: [5, 2, 8]);
        SetupScores((5, 50), (2, 50), (8, 50));

        var sut = CreateSut();
        var result = await sut.HandleAsync(new GetRankingQuery(null, 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal([2, 5, 8], result.Value!.Items.Select(i => i.UserId).ToArray());
    }

    private void SeedProfiles(IEnumerable<int> userIds)
    {
        foreach (var userId in userIds)
        {
            _dbContext.Profiles.Add(new GamificationProfile
            {
                UserId = userId,
                TotalXp = userId * 10,
                Level = 1,
                CurrentStreak = 1,
                ShapeCoins = userId,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }

        _dbContext.SaveChanges();
    }

    private void SetupScores(params (int UserId, int ShapeScore)[] scores)
    {
        foreach (var (userId, shapeScore) in scores)
        {
            _shapeScoreCalculator
                .Setup(x => x.CalculateAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(shapeScore);
        }
    }

    private GetRankingHandler CreateSut() => new(_dbContext, _shapeScoreCalculator.Object);
}
