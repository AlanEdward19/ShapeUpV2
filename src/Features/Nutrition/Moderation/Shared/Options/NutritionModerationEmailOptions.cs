namespace ShapeUp.Features.Nutrition.Moderation.Shared.Options;

public class NutritionModerationEmailOptions
{
    public const string SectionName = "Nutrition:Moderation:Email";

    public string RejectionTemplateId { get; set; } = string.Empty;
    public string RejectionSubject { get; set; } = "Your food edit was not approved";
}
