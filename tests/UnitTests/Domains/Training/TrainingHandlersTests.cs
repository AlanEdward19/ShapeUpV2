using FluentAssertions;
using ShapeUp.Features.Training.Dashboard.GetTrainingDashboard;
using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Shared.Entities;
using ShapeUp.Features.Training.Workouts.CompleteWorkoutSession;
using ShapeUp.Features.Training.Workouts.CreateWorkoutSession;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;
using Xunit;

namespace UnitTests;

public class TrainingHandlersTests
{
    [Fact]
    public async Task CreateWorkout_ShouldFail_WhenActorCannotCreateForTarget()
    {
        var workoutRepository = new FakeWorkoutSessionRepository();
        var exerciseRepository = new FakeExerciseRepository();
        exerciseRepository.AddExercise(new Exercise { Id = 1, Name = "Squat", NamePt = "Agachamento" });

        var handler = new CreateWorkoutSessionHandler(
            workoutRepository,
            exerciseRepository,
            new FakeAccessPolicy(false),
            new CreateWorkoutSessionCommandValidator());

        var command = new CreateWorkoutSessionCommand(
            TargetUserId: 20,
            ExecutedByUserId: 20,
            StartedAtUtc: DateTime.UtcNow,
            Exercises:
            [
                new WorkoutExerciseDto(1,
                [
                    new WorkoutSetValueObject(10, 100, "kg", "working", 8, 120)
                ])
            ]);

        var result = await handler.HandleAsync(command, actorUserId: 5, actorScopes: ["training:workouts:create"], CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task CompleteWorkout_ShouldGeneratePr_WhenHigherLoadExists()
    {
        var now = DateTime.UtcNow;
        var currentSession = new WorkoutSessionDocument
        {
            Id = "507f1f77bcf86cd799439011",
            TargetUserId = 12,
            ExecutedByUserId = 12,
            StartedAtUtc = now.AddMinutes(-30),
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Bench Press",
                    Sets = [new ExecutedSetDocumentValueObject { Load = 110, Repetitions = 5, LoadUnit = "kg", SetType = "working", Rpe = 9, RestSeconds = 120 }]
                }
            ]
        };

        var historicalSession = new WorkoutSessionDocument
        {
            Id = "507f191e810c19729de860ea",
            TargetUserId = 12,
            ExecutedByUserId = 12,
            StartedAtUtc = now.AddDays(-2),
            EndedAtUtc = now.AddDays(-2).AddMinutes(45),
            IsCompleted = true,
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 1,
                    ExerciseName = "Bench Press",
                    Sets = [new ExecutedSetDocumentValueObject { Load = 100, Repetitions = 5, LoadUnit = "kg", SetType = "working", Rpe = 8, RestSeconds = 120 }]
                }
            ]
        };

        var repo = new FakeWorkoutSessionRepository(currentSession, historicalSession);
        var handler = new CompleteWorkoutSessionHandler(repo, new CompleteWorkoutSessionCommandValidator());

        var result = await handler.HandleAsync(
            new CompleteWorkoutSessionCommand(currentSession.Id, now, 8),
            actorUserId: 12,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        repo.LastCompletionUpdate.Should().NotBeNull();
        repo.LastCompletionUpdate!.PersonalRecords.Should().Contain(x => x.Type == "max_load");
    }

    [Fact]
    public async Task Dashboard_ShouldCalculateExpectedWeeklyVolumeAndRate()
    {
        var now = DateTime.UtcNow;
        var monday = now.Date.AddDays(-((7 + (now.DayOfWeek - DayOfWeek.Monday)) % 7));

        var thisWeekSession = new WorkoutSessionDocument
        {
            Id = "507f1f77bcf86cd799439012",
            TargetUserId = 25,
            ExecutedByUserId = 25,
            StartedAtUtc = monday.AddDays(1),
            IsCompleted = true,
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 2,
                    ExerciseName = "Deadlift",
                    Sets =
                    [
                        new ExecutedSetDocumentValueObject { Repetitions = 5, Load = 100, LoadUnit = "kg", SetType = "working", Rpe = 8, RestSeconds = 180 }
                    ]
                }
            ],
            PersonalRecords = [new WorkoutPrDocumentValueObject { ExerciseId = 2, ExerciseName = "Deadlift", Type = "max_volume", Value = 500 }]
        };

        var previousWeekSession = new WorkoutSessionDocument
        {
            Id = "507f1f77bcf86cd799439013",
            TargetUserId = 25,
            ExecutedByUserId = 25,
            StartedAtUtc = monday.AddDays(-6),
            IsCompleted = true,
            Exercises =
            [
                new ExecutedExerciseDocumentValueObject
                {
                    ExerciseId = 2,
                    ExerciseName = "Deadlift",
                    Sets =
                    [
                        new ExecutedSetDocumentValueObject { Repetitions = 5, Load = 80, LoadUnit = "kg", SetType = "working", Rpe = 7, RestSeconds = 180 }
                    ]
                }
            ]
        };

        var repo = new FakeWorkoutSessionRepository(thisWeekSession, previousWeekSession);
        var handler = new GetTrainingDashboardHandler(repo);

        var result = await handler.HandleAsync(new GetTrainingDashboardQuery(25, 4), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.WeeklyVolume.Should().Be(500);
        result.Value.SessionsCompletedThisWeek.Should().Be(1);
        result.Value.SessionsCompletionRate.Should().Be(25);
        result.Value.PersonalRecordsThisWeek.Should().Be(1);
    }

    private sealed class FakeAccessPolicy(bool canCreate) : ITrainingAccessPolicy
    {
        public Task<bool> CanCreateWorkoutForAsync(int actorUserId, int targetUserId, string[] actorScopes, CancellationToken cancellationToken)
            => Task.FromResult(canCreate);
    }

    private sealed class FakeExerciseRepository : IExerciseRepository
    {
        private readonly Dictionary<int, Exercise> _storage = [];

        public void AddExercise(Exercise exercise) => _storage[exercise.Id] = exercise;

        public Task<Exercise?> GetByIdAsync(int id, CancellationToken cancellationToken)
            => Task.FromResult(_storage.TryGetValue(id, out var exercise) ? exercise : null);

        public Task<IReadOnlyList<Exercise>> GetKeysetPageAsync(int? lastId, int pageSize, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Exercise>>([]);

        public Task<IReadOnlyList<Exercise>> SuggestAsync(string name, int[] muscleIds, int[] equipmentIds, int limit, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<Exercise>>([]);

        public Task AddAsync(Exercise exercise, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task UpdateAsync(Exercise exercise, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task DeleteAsync(Exercise exercise, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeWorkoutSessionRepository(params WorkoutSessionDocument[] seed) : IWorkoutSessionRepository
    {
        private readonly List<WorkoutSessionDocument> _sessions = [..seed];

        public CompletionUpdate? LastCompletionUpdate { get; private set; }

        public Task AddAsync(WorkoutSessionDocument session, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(session.Id))
                session.Id = "507f1f77bcf86cd799439099";
            _sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<WorkoutSessionDocument?> GetByIdAsync(string sessionId, CancellationToken cancellationToken)
            => Task.FromResult(_sessions.FirstOrDefault(x => x.Id == sessionId));

        public Task UpdateCompletionAsync(string sessionId, DateTime endedAtUtc, int perceivedExertion, List<WorkoutPrDocumentValueObject> personalRecords, CancellationToken cancellationToken)
        {
            var session = _sessions.First(x => x.Id == sessionId);
            session.EndedAtUtc = endedAtUtc;
            session.PerceivedExertion = perceivedExertion;
            session.IsCompleted = true;
            session.PersonalRecords = personalRecords;

            LastCompletionUpdate = new CompletionUpdate(personalRecords);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<WorkoutSessionDocument>> GetByTargetUserKeysetAsync(int targetUserId, DateTime? startedBeforeUtc, int pageSize, CancellationToken cancellationToken)
        {
            var query = _sessions.Where(x => x.TargetUserId == targetUserId);
            if (startedBeforeUtc.HasValue)
                query = query.Where(x => x.StartedAtUtc < startedBeforeUtc.Value);
            return Task.FromResult<IReadOnlyList<WorkoutSessionDocument>>(query.OrderByDescending(x => x.StartedAtUtc).Take(pageSize).ToArray());
        }

        public Task<IReadOnlyList<WorkoutSessionDocument>> GetCompletedByUserInRangeAsync(int targetUserId, DateTime startInclusiveUtc, DateTime endExclusiveUtc, CancellationToken cancellationToken)
        {
            var items = _sessions
                .Where(x => x.TargetUserId == targetUserId)
                .Where(x => x.IsCompleted || x.EndedAtUtc.HasValue)
                .Where(x => x.StartedAtUtc >= startInclusiveUtc && x.StartedAtUtc < endExclusiveUtc)
                .ToArray();

            return Task.FromResult<IReadOnlyList<WorkoutSessionDocument>>(items);
        }

        public sealed record CompletionUpdate(List<WorkoutPrDocumentValueObject> PersonalRecords);
    }
}




