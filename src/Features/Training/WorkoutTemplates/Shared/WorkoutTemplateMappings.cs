using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Shared.Documents.ValueObjects;
using ShapeUp.Features.Training.WorkoutPlans.Shared.ViewModels;
using ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;
using ShapeUp.Features.Training.Workouts.Shared.ValueObjects;

namespace ShapeUp.Features.Training.WorkoutTemplates.Shared;

public static class WorkoutTemplateMappings
{
    private static BlockDto[] ToBlockDtos(this List<BlockDocumentValueObject> blocks) =>
        blocks
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
                        s.RestSeconds,
                        false)).ToArray(),
                    e.StrengthGainPercentage)).ToArray(),
                b.TimeCapSeconds,
                b.IntervalSeconds,
                b.TotalRounds,
                b.RestAfterSeconds))
            .ToArray();

    public static WorkoutTemplateResponse ToResponse(this WorkoutTemplateDocument template)
    {
        return new WorkoutTemplateResponse(
            template.Id,
            template.CreatedByUserId,
            template.Name,
            template.Notes,
            template.DurationInWeeks,
            template.Phase,
            template.Difficulty,
            template.CreatedAtUtc,
            template.UpdatedAtUtc,
            template.Blocks.ToBlockDtos());
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
            plan.DurationInWeeks,
            plan.Phase,
            plan.Difficulty,
            plan.CreatedAtUtc,
            plan.UpdatedAtUtc,
            plan.Blocks.ToBlockDtos());
    }
}

