namespace FirearmStudio.Application.Model;

public sealed record PaginatedResponse<T> where T : class
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalCount { get; init; }
}
