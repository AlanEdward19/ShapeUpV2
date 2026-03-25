namespace ShapeUp.Shared.Pagination;

public record KeysetPageRequest(string? Cursor, int? PageSize)
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public int NormalizePageSize()
    {
        if (PageSize is null or <= 0)
            return DefaultPageSize;

        return Math.Min(PageSize.Value, MaxPageSize);
    }
}

