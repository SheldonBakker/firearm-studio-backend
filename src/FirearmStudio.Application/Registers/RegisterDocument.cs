namespace FirearmStudio.Application.Registers;

/// <summary>
/// Format-agnostic input for PDF rendering: everything the printed register shows, with all
/// cells already formatted as display strings. Keeps layout inputs unit-testable without
/// touching a PDF library.
/// </summary>
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
