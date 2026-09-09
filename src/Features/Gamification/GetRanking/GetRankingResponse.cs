namespace ShapeUp.Features.Gamification.GetRanking;

public record GetRankingResponse(
    int UserId,
    int ShapeScore,
    int TotalXp,
    int Level,
    int CurrentStreak,
    int ShapeCoins);
