namespace FirearmStudio.Application.Registers;

public sealed record RegisterDocument(
    string Title,
    string CompanyName,
    string? CompanyRegistrationNumber,
    string CompanyAddress,
    DateOnly PeriodFrom,
    DateOnly PeriodTo,
    DateTime GeneratedAt,
    string GeneratedBy,
    IReadOnlyList<string> Columns,
    IReadOnlyList<string[]> Rows,
    string EmptyStateText,
    IReadOnlyList<float>? ColumnWeights = null);
