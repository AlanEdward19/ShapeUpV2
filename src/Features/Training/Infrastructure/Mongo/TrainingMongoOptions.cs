namespace ShapeUp.Features.Training.Infrastructure.Mongo;

public class TrainingMongoOptions
{
    public const string SectionName = "Mongo:Training";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "shapeup";
    public string WorkoutSessionsCollectionName { get; set; } = "workout_sessions";
    public string WorkoutPlansCollectionName { get; set; } = "workout_plans";
    public string WorkoutTemplatesCollectionName { get; set; } = "workout_templates";
}
