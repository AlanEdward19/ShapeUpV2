using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace ShapeUp.Features.Training.WorkoutTemplates.Shared;

public static class WorkoutTemplateMappings
{
    public static WorkoutTemplateResponse ToResponse(this WorkoutTemplateDocument template)
    {
        return new WorkoutTemplateResponse(
            template.Id,
            template.CreatedByUserId,
            template.Name,
            template.Notes,
            template.CreatedAtUtc,
            template.UpdatedAtUtc,
            template.Exercises
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

    public static WorkoutPlanResponse ToPlanResponse(this WorkoutPlanDocument plan)
    {
        return new WorkoutPlanResponse(
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

