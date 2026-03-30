using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates.DeleteWorkoutTemplate;

public class DeleteWorkoutTemplateHandler(IWorkoutTemplateRepository workoutTemplateRepository)
{
    public async Task<Result> HandleAsync(DeleteWorkoutTemplateCommand command, int actorUserId, CancellationToken cancellationToken)
    {
        var template = await workoutTemplateRepository.GetByIdAsync(command.TemplateId, cancellationToken);
        if (template is null)
            return Result.Failure(TrainingErrors.WorkoutTemplateNotFound(command.TemplateId));

        if (template.CreatedByUserId != actorUserId)
            return Result.Failure(TrainingErrors.WorkoutTemplateNotOwned(command.TemplateId, actorUserId));

        await workoutTemplateRepository.DeleteAsync(command.TemplateId, cancellationToken);
        return Result.Success();
    }
}

