namespace ShapeUp.Shared.Pagination;

public record KeysetPageResponse<T>(T[] Items, string? NextCursor);

