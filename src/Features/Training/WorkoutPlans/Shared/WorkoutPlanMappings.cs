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
            DurationInWeeks = source.DurationInWeeks,
            Phase = source.Phase,
            Difficulty = source.Difficulty,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc,
            Blocks = source.Blocks
                .Select(b => new BlockDocumentValueObject
                {
                    Type = b.Type,
                    TimeCapSeconds = b.TimeCapSeconds,
                    IntervalSeconds = b.IntervalSeconds,
                    TotalRounds = b.TotalRounds,
                    RestAfterSeconds = b.RestAfterSeconds,
                    Exercises = b.Exercises
                        .Select(e => new BlockExerciseDocumentValueObject
                        {
                            ExerciseId = e.ExerciseId,
                            ExerciseName = e.ExerciseName,
                            StrengthGainPercentage = e.StrengthGainPercentage,
                            Sets = e.Sets
                                .Select(s => new PlannedSetDocumentValueObject
                                {
                                    Repetitions = s.Repetitions,
                                    Load = s.Load,
                                    LoadUnit = s.LoadUnit,
                                    SetType = s.SetType,
                                    Technique = s.Technique,
                                    Intensity = s.Intensity is null ? null : new IntensityDocumentValueObject { Type = s.Intensity.Type, Value = s.Intensity.Value },
                                    RestSeconds = s.RestSeconds
                                })
                                .ToList()
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
            plan.DurationInWeeks,
            plan.Phase,
            plan.Difficulty,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc,
            plan.Blocks
                .Select(b => new BlockDto(
                    b.Type,
                    b.Exercises.Select(e => new WorkoutExerciseDto(
                        e.ExerciseId,
                        e.Sets.Select(s => new WorkoutSetValueObject(
                            s.Repetitions,
                            s.Load,
                            s.LoadUnit,
                            s.SetType,
                            s.Technique,
                            s.Intensity is null ? null : new IntensityDto(s.Intensity.Type, s.Intensity.Value),
                            s.RestSeconds)).ToArray(),
                        e.StrengthGainPercentage)).ToArray(),
                    b.TimeCapSeconds,
                    b.IntervalSeconds,
                    b.TotalRounds,
                    b.RestAfterSeconds))
                .ToArray());
    }
}
