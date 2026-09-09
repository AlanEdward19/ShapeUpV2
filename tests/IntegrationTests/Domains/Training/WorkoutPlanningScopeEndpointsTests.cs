using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Infrastructure;

namespace IntegrationTests.Domains.Training;

[Collection("SQL Server Write Operations")]
public class WorkoutPlanningScopeEndpointsTests(SqlServerFixture fixture) : IAsyncLifetime
{
    private IntegrationWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new IntegrationWebApplicationFactory(fixture);
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateWorkoutPlan_ForTargetUserWithoutRelationship_ReturnsForbidden()
    {
        // WorkoutPlansController no longer gates on RequireScopesAttribute (native-authorization-model
        // Phase 3, T19): denial now comes from ITrainingAccessPolicy inside CreateWorkoutPlanHandler,
        // which only runs once the request body passes validation. The payload below must therefore be
        // a valid CreateWorkoutPlanCommand (numeric enums, all required fields) targeting a user the
        // actor has no relationship with, to reach the inline authorization check instead of a 400.
        var auth = await SeedUserAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);
        var unrelatedTargetUserId = auth.UserId + 1;

        var body = new
        {
            targetUserId = unrelatedTargetUserId,
            name = "Push Day",
            notes = "notes",
            durationInWeeks = 4,
            phase = "Hypertrophy",
            difficulty = (int)ShapeUp.Features.Training.Shared.Enums.Difficulty.Intermediate,
            blocks = new[]
            {
                new
                {
                    type = (int)ShapeUp.Features.Training.Shared.Enums.BlockType.Straight,
                    exercises = new[]
                    {
                        new
                        {
                            exerciseId = 1,
                            sets = new[]
                            {
                                new
                                {
                                    repetitions = 10,
                                    load = 20m,
                                    loadUnit = (int)ShapeUp.Features.Training.Shared.Enums.LoadUnit.Kg,
                                    setType = (int)ShapeUp.Features.Training.Shared.Enums.SetType.Working,
                                    technique = (int)ShapeUp.Features.Training.Shared.Enums.Technique.Straight,
                                    intensity = new { type = (int)ShapeUp.Features.Training.Shared.Enums.IntensityType.Rpe, value = 8 },
                                    restSeconds = 90
                                }
                            }
                        }
                    }
                }
            }
        };

        var response = await _client.PostAsJsonAsync("/api/training/workout-plans", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ponytail: "CreateWorkoutTemplate_WithoutScope_ReturnsForbidden" removed — WorkoutTemplatesController no
    // longer carries RequireScopesAttribute (native-authorization-model Phase 3). Create is always
    // self-scoped (no target user), so authentication alone is sufficient; scope-denial coverage for
    // this endpoint is gone by design. Owner/non-owner authorization coverage for the other
    // WorkoutTemplates endpoints lives in Endpoints/WorkoutTemplatesEndpointsIntegrationTests.cs.

    private async Task<(int UserId, string Token)> SeedUserAsync()
    {
        await using var context = fixture.CreateAuthorizationDbContext();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var user = await TestDataSeeder.SeedUserAsync(context, suffix, CancellationToken.None);
        return (user.Id, TestFirebaseService.CreateToken(user.FirebaseUid, user.Email));
    }
}


