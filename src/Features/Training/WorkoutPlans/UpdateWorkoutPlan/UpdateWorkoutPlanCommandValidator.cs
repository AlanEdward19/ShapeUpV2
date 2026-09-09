using FluentValidation;
using ShapeUp.Features.Training.Shared.Enums;

namespace ShapeUp.Features.Training.WorkoutPlans.UpdateWorkoutPlan;

public class UpdateWorkoutPlanCommandValidator : AbstractValidator<UpdateWorkoutPlanCommand>
{
    public UpdateWorkoutPlanCommandValidator()
    {
        RuleFor(x => x.GetPlanId()).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(2000);
        RuleFor(x => x.DurationInWeeks).GreaterThan(0).LessThanOrEqualTo(52);
        RuleFor(x => x.Phase).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Blocks).NotEmpty().WithMessage("Workout plan must have at least one block");

        RuleForEach(x => x.Blocks).ChildRules(block =>
        {
            block.RuleFor(b => b.Type).IsInEnum();
            block.RuleFor(b => b.Exercises).NotEmpty();
            block.RuleFor(b => b.Exercises)
                .Must(exercises => exercises.Length >= 2)
                .When(b => b.Type == BlockType.Superset)
                .WithMessage("Superset precisa de pelo menos 2 exercícios");
            block.RuleFor(b => b.TimeCapSeconds)
                .GreaterThan(0)
                .When(b => b.Type == BlockType.Amrap)
                .WithMessage("Amrap precisa de um tempo-limite (TimeCapSeconds) maior que zero");
            block.RuleFor(b => b.IntervalSeconds)
                .GreaterThan(0)
                .When(b => b.Type == BlockType.Emom)
                .WithMessage("Emom precisa de um intervalo (IntervalSeconds) maior que zero");
            block.RuleFor(b => b.TotalRounds)
                .GreaterThan(0)
                .When(b => b.Type == BlockType.Emom)
                .WithMessage("Emom precisa de um número de rounds (TotalRounds) maior que zero");
            block.RuleFor(b => b)
                .Must(b => b.Type == BlockType.Straight || b.Exercises.All(e => e.Sets.All(s => s.RestSeconds == null)))
                .WithMessage("RestSeconds só é válido em blocos Straight");

            block.RuleForEach(b => b.Exercises).ChildRules(exercise =>
            {
                exercise.RuleFor(x => x.ExerciseId).GreaterThan(0);
                exercise.RuleFor(x => x.Sets).NotEmpty();
                exercise.RuleFor(x => x.StrengthGainPercentage)
                    .InclusiveBetween(0, 100)
                    .When(x => x.StrengthGainPercentage.HasValue);

                exercise.RuleForEach(x => x.Sets).ChildRules(set =>
                {
                    set.RuleFor(x => x.Repetitions!.Value).GreaterThan(0).When(x => x.Repetitions.HasValue);
                    set.RuleFor(x => x.Load).GreaterThanOrEqualTo(0);
                    set.RuleFor(x => x.LoadUnit).IsInEnum();
                    set.RuleFor(x => x.SetType).IsInEnum();
                    set.RuleFor(x => x.Technique).IsInEnum();
                    set.RuleFor(x => x.Intensity!.Value).InclusiveBetween(1, 10).When(x => x.Intensity != null);
                    set.RuleFor(x => x.RestSeconds!.Value).GreaterThanOrEqualTo(0).When(x => x.RestSeconds.HasValue);
                });
            });
        });
    }
}
