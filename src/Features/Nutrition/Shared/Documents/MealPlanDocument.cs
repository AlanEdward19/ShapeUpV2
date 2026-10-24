using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ShapeUp.Features.Nutrition.Shared.Documents;

public class MealPlanDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    public int UserId { get; set; }
    public string Name { get; set; } = null!;
    public int? PrescribedByRelationshipId { get; set; }
    /// <summary>The professional who prescribed the plan; null when the user created it themselves.</summary>
    public int? PrescribedByUserId { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<MealPlanItemDocument> Items { get; set; } = [];
    /// <summary>Optional per-slot macro targets. A slot with targets and no items is "macros only": the client structures it.</summary>
    public List<MealPlanMealTargetDocument> Meals { get; set; } = [];
}

public class MealPlanMealTargetDocument
{
    public string MealSlot { get; set; } = null!;
    public int? TargetCalories { get; set; }
    public int? TargetProteinG { get; set; }
    public int? TargetCarbsG { get; set; }
    public int? TargetFatG { get; set; }
}

public class MealPlanItemDocument
{
    public string MealSlot { get; set; } = null!;
    public string FoodId { get; set; } = null!;
    public decimal QuantityGramsOrMl { get; set; }
}
