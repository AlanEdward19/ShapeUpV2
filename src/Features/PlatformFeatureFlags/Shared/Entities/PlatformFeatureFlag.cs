namespace ShapeUp.Features.PlatformFeatureFlags.Shared.Entities;

public class PlatformFeatureFlag
{
    public string Key { get; set; } = null!;
    public bool Enabled { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public int? UpdatedByUserId { get; set; }
}
