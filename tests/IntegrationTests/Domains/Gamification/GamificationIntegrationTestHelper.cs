namespace IntegrationTests.Domains.Gamification;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Domains.Messaging;
using IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ShapeUp.Features.Gamification.Shared.Entities;
using ShapeUp.Features.Gamification.Shared.Enums;
using ShapeUp.Features.Training.Shared.Enums;

internal static class GamificationIntegrationTestHelper
{
    internal const int PlausibleWorkoutDurationSeconds = 120;
    internal const int DefaultRestSeconds = 90;
    internal const int DefaultRepetitions = 10;
    internal const decimal DefaultLoad = 20m;

    internal static async Task WaitForConsumerLogAsync(string sessionId, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (WorkoutFinishedConsumerLogCapture.TryGetCaptured(sessionId, out _))
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"WorkoutFinishedConsumer did not log session {sessionId} within {timeout.TotalSeconds}s.");
    }

    internal static async Task<(int UserId, string Token)> SeedTrainingUserAsync(SqlServerFixture sqlFixture)
    {
        await using var context = sqlFixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);

        await using var gymContext = sqlFixture.CreateGymManagementDbContext();
        await TestDataSeeder.GrantPlatformAdminAsync(gymContext, user.Id, CancellationToken.None);

        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }

    internal static void Authorize(HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    internal static async Task<int> CreateExerciseAsync(HttpClient client)
    {
        var equipment = await client.PostAsJsonAsync("/api/training/equipments", new
        {
            name = $"Barbell-{Guid.NewGuid():N}",
            namePt = $"Barra-{Guid.NewGuid():N}",
            description = "Olympic barbell"
        });
        equipment.EnsureSuccessStatusCode();
        var equipmentId = (await equipment.Content.ReadFromJsonAsync<EquipmentPayload>())!.Id;

        var exercise = await client.PostAsJsonAsync("/api/training/exercises", new
        {
            name = $"Bench Press-{Guid.NewGuid():N}",
            namePt = $"Supino-{Guid.NewGuid():N}",
            description = "Compound press",
            videoUrl = (string?)null,
            muscles = new[] { new { muscleGroup = (int)MuscleGroup.Chest, activationPercent = 70m } },
            equipmentIds = new[] { equipmentId },
            steps = new[] { new { description = "Brace and press" } }
        });
        exercise.EnsureSuccessStatusCode();
        return (await exercise.Content.ReadFromJsonAsync<ExercisePayload>())!.Id;
    }

    internal static async Task<string> CreatePlanAsync(
        HttpClient client,
        int targetUserId,
        int exerciseId,
        decimal load = DefaultLoad,
        int repetitions = DefaultRepetitions,
        int restSeconds = DefaultRestSeconds)
    {
        var response = await client.PostAsJsonAsync("/api/training/workout-plans", new
        {
            targetUserId,
            name = $"Plan-{Guid.NewGuid():N}",
            notes = "notes",
            durationInWeeks = 4,
            phase = "Hypertrophy",
            difficulty = (int)Difficulty.Intermediate,
            blocks = new[]
            {
                new
                {
                    type = (int)BlockType.Straight,
                    exercises = new[]
                    {
                        new
                        {
                            exerciseId,
                            sets = new[]
                            {
                                new
                                {
                                    repetitions,
                                    load,
                                    loadUnit = (int)LoadUnit.Kg,
                                    setType = (int)SetType.Working,
                                    technique = (int)Technique.Straight,
                                    intensity = new { type = (int)IntensityType.Rpe, value = 8 },
                                    restSeconds
                                }
                            }
                        }
                    }
                }
            }
        });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<WorkoutPlanPayload>();
        return payload!.PlanId;
    }

    internal static async Task<string> StartSessionAsync(
        HttpClient client,
        string planId,
        int executedByUserId,
        DateTime startedAtUtc)
    {
        var response = await client.PostAsJsonAsync("/api/training/workouts/start", new
        {
            planId,
            startedAtUtc,
            executedByUserId
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<WorkoutPayload>();
        return payload!.SessionId;
    }

    internal static async Task FinishSessionAsync(
        HttpClient client,
        string sessionId,
        DateTime endedAtUtc,
        object? exercises = null)
    {
        object body = exercises is null
            ? new { endedAtUtc, perceivedExertion = 7 }
            : new { endedAtUtc, perceivedExertion = 7, exercises };

        var response = await client.PostAsJsonAsync($"/api/training/workouts/{sessionId}/finish", body);
        response.EnsureSuccessStatusCode();
    }

    internal static async Task<(string SessionId, DateTime EndedAtUtc)> FinishPlausibleWorkoutAsync(
        HttpClient client,
        int userId,
        int exerciseId,
        decimal load = DefaultLoad,
        int repetitions = DefaultRepetitions,
        int restSeconds = DefaultRestSeconds,
        DateTime? endedAtUtc = null,
        object? finishExercises = null)
    {
        var planId = await CreatePlanAsync(client, userId, exerciseId, load, repetitions, restSeconds);
        var endedAt = endedAtUtc ?? DateTime.UtcNow;
        var startedAt = endedAt.AddSeconds(-PlausibleWorkoutDurationSeconds);
        var sessionId = await StartSessionAsync(client, planId, userId, startedAt);
        await FinishSessionAsync(client, sessionId, endedAt, finishExercises);
        return (sessionId, endedAt);
    }

    internal static async Task<GamificationProfilePayload> GetProfileAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/gamification/me");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<GamificationProfilePayload>())!;
    }

    internal static async Task<WorkoutEvaluation?> GetEvaluationAsync(
        SqlServerFixture sqlFixture,
        string sessionId)
    {
        await using var context = sqlFixture.CreateGamificationDbContext();
        return await context.Evaluations.AsNoTracking()
            .FirstOrDefaultAsync(e => e.SessionId == sessionId);
    }

    internal static async Task<WorkoutEvaluation> WaitForEvaluationAsync(
        SqlServerFixture sqlFixture,
        string sessionId,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            var evaluation = await GetEvaluationAsync(sqlFixture, sessionId);
            if (evaluation is not null)
                return evaluation;

            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException($"WorkoutEvaluation for session {sessionId} was not persisted within {timeout.TotalSeconds}s.");
    }

    internal static object BuildFinishExercises(int exerciseId, decimal load, int repetitions, int restSeconds = DefaultRestSeconds) =>
        new[]
        {
            new
            {
                exerciseId,
                sets = new[]
                {
                    new
                    {
                        repetitions,
                        load,
                        loadUnit = (int)LoadUnit.Kg,
                        setType = (int)SetType.Working,
                        technique = (int)Technique.Straight,
                        intensity = new { type = (int)IntensityType.Rpe, value = 8 },
                        restSeconds
                    }
                }
            }
        };

    internal sealed record EquipmentPayload(int Id, string Name, string NamePt, string? Description);
    internal sealed record ExercisePayload(int Id, string Name, string NamePt);
    internal sealed record WorkoutPlanPayload(string PlanId, int TargetUserId, string Name);
    internal sealed record WorkoutPayload(string SessionId, int TargetUserId, bool IsCompleted);

    internal sealed record GamificationProfilePayload(
        int TotalXp,
        int Level,
        int CurrentStreak,
        int ShapeCoins,
        int ShapeScore,
        bool LastEvaluationLeveledUp,
        int? LastEvaluationLevelFrom,
        int? LastEvaluationLevelTo,
        bool LastEvaluationStreakMilestoneHit,
        int? LastEvaluationStreakMilestoneValue);

    internal sealed record RankingPagePayload(
        RankingEntryPayload[] Items,
        string? NextCursor);

    internal sealed record RankingEntryPayload(
        int UserId,
        int ShapeScore,
        int TotalXp,
        int Level,
        int CurrentStreak,
        int ShapeCoins);

    internal static void AssertClassification(
        WorkoutEvaluation? evaluation,
        ActivityClassification expectedClassification,
        bool expectedCreditGranted)
    {
        Assert.NotNull(evaluation);
        Assert.Equal(expectedClassification, evaluation!.Classification);
        Assert.Equal(expectedCreditGranted, evaluation.CreditGranted);
    }
}
