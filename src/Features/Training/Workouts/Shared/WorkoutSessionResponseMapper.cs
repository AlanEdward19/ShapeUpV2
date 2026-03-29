using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;

namespace ShapeUp.Features.Training.Workouts.Shared;

public class WorkoutSessionResponseMapper : IWorkoutSessionResponseMapper
{
    public WorkoutSessionResponse Map(WorkoutSessionDocument session) =>
        new(
            session.Id,
            session.WorkoutPlanId,
            session.TargetUserId,
            session.ExecutedByUserId,
            session.TrainerUserId,
            session.StartedAtUtc,
            session.LastSavedAtUtc,
            session.EndedAtUtc,
            session.DurationSeconds,
            session.PerceivedExertion,
            session.IsCompleted,
            session.Exercises
                .Select(exercise => new ExecutedExerciseDto(
                    exercise.ExerciseId,
                    exercise.ExerciseName,
                    exercise.Sets.Select(set => new ExecutedSetValueObject(
                        set.Repetitions,
                        set.Load,
                        set.LoadUnit,
                        set.SetType,
                        set.Rpe,
                        set.RestSeconds,
                        set.Volume,
                        set.IsExtra)).ToArray()))
                .ToArray(),
            session.PersonalRecords.Select(pr => new WorkoutPrDto(pr.ExerciseId, pr.ExerciseName, pr.Type, pr.Value)).ToArray());
}

