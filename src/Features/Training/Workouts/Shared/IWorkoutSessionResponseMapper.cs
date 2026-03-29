using ShapeUp.Features.Training.Shared.Documents;
using ShapeUp.Features.Training.Workouts.Shared.ViewModels;

namespace ShapeUp.Features.Training.Workouts.Shared;

public interface IWorkoutSessionResponseMapper
{
    WorkoutSessionResponse Map(WorkoutSessionDocument session);
}

