namespace ShapeUp.Features.Nutrition.Infrastructure.Mongo;

public class NutritionMongoOptions
{
    public const string SectionName = "Mongo:Nutrition";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017/?replicaSet=rs0&directConnection=true";
    public string DatabaseName { get; set; } = "shapeup";
    public string WeightTargetsCollectionName { get; set; } = "weight_targets";
    public string WeightRegistersCollectionName { get; set; } = "weight_registers";
    public string FoodsCollectionName { get; set; } = "foods";
    public string FoodOverridesCollectionName { get; set; } = "food_overrides";
    public string FoodModerationRequestsCollectionName { get; set; } = "food_moderation_requests";
    public string MealPlansCollectionName { get; set; } = "meal_plans";
}
