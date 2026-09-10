namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class NutritionMongoOptions
{
    public const string SectionName = "Mongo:Nutrition";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017/?replicaSet=rs0&directConnection=true";
    public string DatabaseName { get; set; } = "shapeup";
    public string WeightTargetsCollectionName { get; set; } = "weight_targets";
    public string WeightRegistersCollectionName { get; set; } = "weight_registers";
}
