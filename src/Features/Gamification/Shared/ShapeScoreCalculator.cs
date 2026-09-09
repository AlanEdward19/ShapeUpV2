namespace ShapeUp.Features.Gamification.Shared;

using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Entities;
using Shared.Enums;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;

/// <summary>
/// Computes ShapeScore v1 over a rolling 30-calendar-day window (UTC).
/// Final score is the arithmetic mean of four 0–1 sub-scores, scaled to 0–100
/// and rounded with <see cref="MidpointRounding.AwayFromZero"/>.
/// </summary>
public sealed class ShapeScoreCalculator(
    GamificationDbContext dbContext,
    IWorkoutSessionRepository workoutSessionRepository) : IShapeScoreCalculator
{
    public const int DefaultSessionsTargetPerWeek = 3;
    private const int WindowDays = 30;

    public async Task<int> CalculateAsync(int userId, CancellationToken cancellationToken)
    {
        var windowEndUtc = DateTime.UtcNow;
        var windowStartUtc = windowEndUtc.AddDays(-WindowDays);

        var evaluations = await dbContext.Evaluations
            .AsNoTracking()
            .Where(e => e.UserId == userId
                        && e.EvaluatedAtUtc >= windowStartUtc
                        && e.EvaluatedAtUtc <= windowEndUtc)
            .ToListAsync(cancellationToken);

        var sessions = await workoutSessionRepository.GetCompletedByUserInRangeAsync(
            userId,
            windowStartUtc,
            windowEndUtc,
            cancellationToken);

        if (evaluations.Count == 0 && sessions.Count == 0)
            return 0;

        var evaluationsBySessionId = evaluations.ToDictionary(e => e.SessionId);

        var consistencia = CalculateConsistencia(evaluations);
        var evolucao = CalculateEvolucao(sessions);
        var metas = CalculateMetas(sessions, windowStartUtc, windowEndUtc);
        var verificacao = CalculateVerificacao(sessions, evaluationsBySessionId);

        var average = (consistencia + evolucao + metas + verificacao) / 4m;
        return (int)Math.Round(average * 100m, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Distinct UTC days with at least one Verified/LikelyValid evaluation, divided by 30.
    /// </summary>
    private static decimal CalculateConsistencia(IReadOnlyList<WorkoutEvaluation> evaluations)
    {
        var legitimateDays = evaluations
            .Where(e => e.Classification is ActivityClassification.Verified or ActivityClassification.LikelyValid)
            .Select(e => e.EvaluatedAtUtc.Date)
            .Distinct()
            .Count();

        return legitimateDays / (decimal)WindowDays;
    }

    /// <summary>
    /// 1 when any completed session in the window recorded new personal records; otherwise 0.
    /// </summary>
    private static decimal CalculateEvolucao(IReadOnlyList<WorkoutSessionDocument> sessions) =>
        sessions.Any(s => s.PersonalRecords.Count > 0) ? 1m : 0m;

    /// <summary>
    /// Fraction of Monday-start UTC week buckets overlapping the window where completed session count
    /// within the bucket meets <see cref="DefaultSessionsTargetPerWeek"/>.
    /// </summary>
    private static decimal CalculateMetas(
        IReadOnlyList<WorkoutSessionDocument> sessions,
        DateTime windowStartUtc,
        DateTime windowEndUtc)
    {
        var weekBuckets = GetWeekBuckets(windowStartUtc, windowEndUtc);
        if (weekBuckets.Count == 0)
            return 0m;

        var weeksMeetingTarget = weekBuckets.Count(bucket =>
            CountSessionsInRange(sessions, bucket.StartUtc, bucket.EndUtc) >= DefaultSessionsTargetPerWeek);

        return weeksMeetingTarget / (decimal)weekBuckets.Count;
    }

    /// <summary>
    /// Fraction of completed sessions in the window classified as Verified/LikelyValid.
    /// Returns 0 when there are no sessions in the window.
    /// </summary>
    private static decimal CalculateVerificacao(
        IReadOnlyList<WorkoutSessionDocument> sessions,
        IReadOnlyDictionary<string, WorkoutEvaluation> evaluationsBySessionId)
    {
        if (sessions.Count == 0)
            return 0m;

        var verifiedSessions = sessions.Count(session =>
            evaluationsBySessionId.TryGetValue(session.Id, out var evaluation)
            && evaluation.Classification is ActivityClassification.Verified or ActivityClassification.LikelyValid);

        return verifiedSessions / (decimal)sessions.Count;
    }

    private static int CountSessionsInRange(
        IReadOnlyList<WorkoutSessionDocument> sessions,
        DateTime rangeStartInclusiveUtc,
        DateTime rangeEndExclusiveUtc) =>
        sessions.Count(session =>
            session.StartedAtUtc >= rangeStartInclusiveUtc
            && session.StartedAtUtc < rangeEndExclusiveUtc);

    private static IReadOnlyList<(DateTime StartUtc, DateTime EndUtc)> GetWeekBuckets(
        DateTime windowStartUtc,
        DateTime windowEndUtc)
    {
        var buckets = new List<(DateTime StartUtc, DateTime EndUtc)>();

        for (var weekStart = StartOfWeekUtc(windowStartUtc.Date);
             weekStart < windowEndUtc;
             weekStart = weekStart.AddDays(7))
        {
            var weekEnd = weekStart.AddDays(7);
            var bucketStart = weekStart < windowStartUtc ? windowStartUtc : weekStart;
            var bucketEnd = weekEnd > windowEndUtc ? windowEndUtc : weekEnd;

            if (bucketStart < bucketEnd)
                buckets.Add((bucketStart, bucketEnd));
        }

        return buckets;
    }

    private static DateTime StartOfWeekUtc(DateTime dateUtc)
    {
        var diff = (7 + (dateUtc.DayOfWeek - DayOfWeek.Monday)) % 7;
        return dateUtc.AddDays(-diff);
    }
}
