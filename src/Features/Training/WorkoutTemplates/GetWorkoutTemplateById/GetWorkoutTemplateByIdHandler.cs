using ShapeUp.Features.Training.Shared.Abstractions;
using ShapeUp.Features.Training.Shared.Errors;
using ShapeUp.Features.Training.WorkoutTemplates.Shared;
using ShapeUp.Features.Training.WorkoutTemplates.Shared.ViewModels;
using ShapeUp.Shared.Results;

namespace ShapeUp.Features.Training.WorkoutTemplates.GetWorkoutTemplateById;

public class GetWorkoutTemplateByIdHandler(IWorkoutTemplateRepository workoutTemplateRepository)
{
    public async Task<Result<WorkoutTemplateResponse>> HandleAsync(GetWorkoutTemplateByIdQuery query, int actorUserId, CancellationToken cancellationToken)
    {
        var template = await workoutTemplateRepository.GetByIdAsync(query.TemplateId, cancellationToken);
        if (template is null)
            return Result<WorkoutTemplateResponse>.Failure(TrainingErrors.WorkoutTemplateNotFound(query.TemplateId));

        if (template.CreatedByUserId != actorUserId)
            return Result<WorkoutTemplateResponse>.Failure(CommonErrors.Forbidden("You are not allowed to access this workout template."));

        return Result<WorkoutTemplateResponse>.Success(template.ToResponse());
    }
}

