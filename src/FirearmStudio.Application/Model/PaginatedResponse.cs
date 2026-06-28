namespace FirearmStudio.Application.Model;

public record PaginatedResponse<T> where T : class
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }

    public static PaginatedResponse<T> Empty() => new()
    {
        Items = [],
        PageNumber = 0,
        PageSize = 0,
        TotalCount = 0,
    };
}
