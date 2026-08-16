using FirearmStudio.Application.Common;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Registers;

public static class RegisterDocumentFactory
{
    private const string EmptyState = "No movements in period.";
    private const string WetInkSignatureColumn = "Signature";

    private static readonly float[] FirearmsColumnWeights =
        [0.8f, 0.8f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.9f, 0.9f, 0.9f, 0.9f];

    private static readonly float[] SafeCustodyColumnWeights =
        [0.9f, 0.9f, 0.9f, 0.9f, 0.8f, 1.1f, 1.3f, 1.2f, 1.8f, 1.2f, 0.9f, 0.8f, 0.8f, 1.2f, 0.9f, 1.0f];

    public static RegisterDocument Create(
        RegisterKind kind,
        IReadOnlyList<StorageRegisterRow> rows,
        Company company,
        DateOnly from,
        DateOnly to,
        DateTime generatedAtUtc,
        string generatedBy)
    {
        string title;
        IReadOnlyList<string> columns;
        Func<StorageRegisterRow, string[]> formatRow;
        IReadOnlyList<float> columnWeights;

        if (kind == RegisterKind.Firearms)
        {
            title = "Firearms Register";
            columns = FirearmsRegisterCsvBuilder.Headers;
            formatRow = FirearmsRegisterCsvBuilder.FormatRow;
            columnWeights = FirearmsColumnWeights;
        }
        else
        {
            title = "Safe Custody Register";
            columns = [.. SafeCustodyRegisterCsvBuilder.Headers, WetInkSignatureColumn];
            formatRow = row => [.. SafeCustodyRegisterCsvBuilder.FormatRow(row), string.Empty];
            columnWeights = SafeCustodyColumnWeights;
        }

        return new RegisterDocument(
            title,
            company.Name,
            company.RegistrationNumber,
            ComposeAddress(company),
            from,
            to,
            TimeZoneInfo.ConvertTimeFromUtc(generatedAtUtc, SouthAfricaTimeZone.Instance),
            generatedBy,
            columns,
            rows.Select(formatRow).ToList(),
            EmptyState,
            columnWeights);
    }

    private static string ComposeAddress(Company company) => string.Join(", ",
        new[] { company.AddressLine1, company.AddressLine2, company.City, company.Province, company.PostalCode }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
}
