using FluentValidation;
using ShapeUp.Features.Training.Workouts.Shared.Dtos;

namespace ShapeUp.Features.Training.Workouts.UpdateWorkoutExecutionState;

public class UpdateWorkoutExecutionStateCommandValidator : AbstractValidator<UpdateWorkoutExecutionStateCommand>
{
    public UpdateWorkoutExecutionStateCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
        RuleFor(x => x.SavedAtUtc).NotNull();
        RuleFor(x => x.Exercises).NotEmpty();

        RuleForEach(x => x.Exercises).SetValidator(new WorkoutExerciseDtoValidator());
    }
}
