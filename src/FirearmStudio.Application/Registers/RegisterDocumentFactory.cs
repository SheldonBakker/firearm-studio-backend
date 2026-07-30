using FirearmStudio.Application.Common;
using FirearmStudio.Domain.Entities;

namespace FirearmStudio.Application.Registers;

public static class RegisterDocumentFactory
{
    private const string EmptyState = "No movements in period.";

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

        if (kind == RegisterKind.Firearms)
        {
            title = "Firearms Register";
            columns = FirearmsRegisterCsvBuilder.Headers;
            formatRow = FirearmsRegisterCsvBuilder.FormatRow;
        }
        else
        {
            // The printed safe custody register carries a blank column for wet-ink sign-off.
            title = "Safe Custody Register";
            columns = [.. SafeCustodyRegisterCsvBuilder.Headers, "Signature"];
            formatRow = row => [.. SafeCustodyRegisterCsvBuilder.FormatRow(row), string.Empty];
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
            EmptyState);
    }

    private static string ComposeAddress(Company company) => string.Join(", ",
        new[] { company.AddressLine1, company.AddressLine2, company.City, company.Province, company.PostalCode }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
}
