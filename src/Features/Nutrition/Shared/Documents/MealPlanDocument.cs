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
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public List<MealPlanItemDocument> Items { get; set; } = [];
}

public class MealPlanItemDocument
{
    public string MealSlot { get; set; } = null!;
    public string FoodId { get; set; } = null!;
    public decimal QuantityGramsOrMl { get; set; }
}
