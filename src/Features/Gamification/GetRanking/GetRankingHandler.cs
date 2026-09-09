namespace ShapeUp.Features.Gamification.GetRanking;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.Entities;
using ShapeUp.Shared.Pagination;
using ShapeUp.Shared.Results;

public sealed class GetRankingHandler(
    GamificationDbContext dbContext,
    IShapeScoreCalculator shapeScoreCalculator)
{
    /// <summary>
    /// Global ranking ordered by ShapeScore desc, UserId asc (stable keyset tie-breaker).
    /// ShapeScore is computed at read time; only users with a GamificationProfile row are ranked.
    /// </summary>
    public async Task<Result<KeysetPageResponse<GetRankingResponse>>> HandleAsync(
        GetRankingQuery query,
        CancellationToken cancellationToken)
    {
        int? cursorShapeScore = null;
        int? cursorUserId = null;

        if (!string.IsNullOrWhiteSpace(query.Cursor))
        {
            if (!RankingCursorCodec.TryDecode(query.Cursor, out var decodedScore, out var decodedUserId))
                return Result<KeysetPageResponse<GetRankingResponse>>.Failure(CommonErrors.Validation("Invalid cursor."));

            cursorShapeScore = decodedScore;
            cursorUserId = decodedUserId;
        }

        var pageSize = new KeysetPageRequest(query.Cursor, query.PageSize).NormalizePageSize();

        var profiles = await dbContext.Profiles
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rankedEntries = new List<RankedEntry>(profiles.Count);
        foreach (var profile in profiles)
        {
            var shapeScore = await shapeScoreCalculator.CalculateAsync(profile.UserId, cancellationToken);
            rankedEntries.Add(new RankedEntry(profile, shapeScore));
        }

        IEnumerable<RankedEntry> ordered = rankedEntries
            .OrderByDescending(e => e.ShapeScore)
            .ThenBy(e => e.Profile.UserId);

        if (cursorShapeScore.HasValue && cursorUserId.HasValue)
        {
            ordered = ordered.Where(e =>
                e.ShapeScore < cursorShapeScore.Value
                || (e.ShapeScore == cursorShapeScore.Value && e.Profile.UserId > cursorUserId.Value));
        }

        var page = ordered.Take(pageSize).ToArray();
        var items = page
            .Select(e => new GetRankingResponse(
                e.Profile.UserId,
                e.ShapeScore,
                e.Profile.TotalXp,
                e.Profile.Level,
                e.Profile.CurrentStreak,
                e.Profile.ShapeCoins))
            .ToArray();

        var nextCursor = page.Length < pageSize
            ? null
            : RankingCursorCodec.Encode(page[^1].ShapeScore, page[^1].Profile.UserId);

        return Result<KeysetPageResponse<GetRankingResponse>>.Success(
            new KeysetPageResponse<GetRankingResponse>(items, nextCursor));
    }

    private sealed record RankedEntry(GamificationProfile Profile, int ShapeScore);
}
