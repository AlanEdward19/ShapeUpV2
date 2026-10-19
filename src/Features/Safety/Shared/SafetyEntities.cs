namespace ShapeUp.Features.Safety.Shared;

/// <summary>The blocker no longer sees, receives invites from, or stays linked to the blocked user.</summary>
public class UserBlock
{
    public int Id { get; set; }
    public int BlockerUserId { get; set; }
    public int BlockedUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public static class ReportReasons
{
    public const string Spam = "Spam";
    public const string Harassment = "Harassment";
    public const string Inappropriate = "Inappropriate";
    public const string Impersonation = "Impersonation";
    public const string Other = "Other";

    public static readonly string[] All = [Spam, Harassment, Inappropriate, Impersonation, Other];
}

public static class ReportTargets
{
    public const string User = "User";
    public const string Message = "Message";
    public const string Comment = "Comment";
    public const string Food = "Food";
    public const string Other = "Other";

    public static readonly string[] All = [User, Message, Comment, Food, Other];
}

public static class ReportStatuses
{
    public const string Open = "Open";
    public const string Resolved = "Resolved";
    public const string Dismissed = "Dismissed";
}

/// <summary>A user's report of offensive content or behavior; a platform admin reviews it (the App Store expects action within 24 hours).</summary>
public class ContentReport
{
    public int Id { get; set; }
    public int ReporterUserId { get; set; }
    public required string TargetType { get; set; }
    public string? TargetId { get; set; }
    public int? TargetUserId { get; set; }
    public required string Reason { get; set; }
    public string? Details { get; set; }
    public string Status { get; set; } = ReportStatuses.Open;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public int? ResolvedByUserId { get; set; }
    public string? ResolutionNote { get; set; }
}

public record ReportResponse(
    int Id, string TargetType, string? TargetId, int? TargetUserId, string Reason, string? Details, string Status,
    DateTime CreatedAtUtc, DateTime? ResolvedAtUtc, string? ResolutionNote, int? ReporterUserId = null);

public record BlockResponse(int BlockedUserId, string? Name, DateTime CreatedAtUtc);
