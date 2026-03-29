using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace ShapeUp.Features.Training.WorkoutPlans.Shared;

public static class WorkoutPlanMappings
{
    public static WorkoutPlanDocument Clone(this WorkoutPlanDocument source, int targetUserId, int actorUserId, string name, DateTime nowUtc)
    {
        return new WorkoutPlanDocument
        {
            TargetUserId = targetUserId,
            CreatedByUserId = actorUserId,
            TrainerUserId = actorUserId == targetUserId ? null : actorUserId,
            Name = name,
            Notes = source.Notes,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Exercises = source.Exercises
                .Select(e => new PlannedExerciseDocumentValueObject
                {
                    ExerciseId = e.ExerciseId,
                    ExerciseName = e.ExerciseName,
                    Sets = e.Sets
                        .Select(s => new PlannedSetDocumentValueObject
                        {
                            Repetitions = s.Repetitions,
                            Load = s.Load,
                            LoadUnit = s.LoadUnit,
                            SetType = s.SetType,
                            Rpe = s.Rpe,
                            RestSeconds = s.RestSeconds
                        })
                        .ToList()
                })
                .ToList()
        };
    }

    public static ViewModels.WorkoutPlanResponse ToResponse(this WorkoutPlanDocument plan)
    {
        return new ViewModels.WorkoutPlanResponse(
            plan.Id,
            plan.TargetUserId,
            plan.CreatedByUserId,
            plan.TrainerUserId,
            plan.Name,
            plan.Notes,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc,
            plan.Exercises
                .Select(e => new WorkoutExerciseDto(
                    e.ExerciseId,
                    e.Sets.Select(s => new WorkoutSetValueObject(
                        s.Repetitions,
                        s.Load,
                        s.LoadUnit,
                        s.SetType,
                        s.Rpe,
                        s.RestSeconds,
                        false)).ToArray()))
                .ToArray());
    }
}
