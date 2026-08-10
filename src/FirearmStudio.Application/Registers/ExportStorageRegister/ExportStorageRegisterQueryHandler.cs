using System.Text.Json;
using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Registers.ExportStorageRegister;

public sealed class ExportStorageRegisterQueryHandler(
    IApplicationDbContext db,
    ITenantContext tenant,
    ICurrentUserService currentUserService,
    ICredentialProtector credentialProtector,
    IRegisterPdfRenderer pdfRenderer)
    : IQueryHandler<ExportStorageRegisterQuery, ErrorOr<RegisterExportResult>>
{
    internal const int MaxExportRows = 20000;
    internal const int MaxPdfExportRows = 2000;

    public async Task<ErrorOr<RegisterExportResult>> Handle(
        ExportStorageRegisterQuery query, CancellationToken cancellationToken)
    {
        if (query.From > query.To)
        {
            return Error.Validation(ErrorCodes.InvalidRange, "'from' must be on or before 'to'.");
        }

        if (tenant.CompanyId is not { } companyId)
        {
            return Error.Forbidden(ErrorCodes.NoCompany, "The current user has no company scope.");
        }

        var queryable = db.StorageRecords
            .AsNoTracking()
            .Where(StorageRecordPeriod.OverlapsRange(query.From, query.To));

        var totalCount = await queryable.CountAsync(cancellationToken);

        if (RowCapError(query.Format, totalCount) is { } rowCapError)
        {
            return rowCapError;
        }

        var rows = await queryable
            .OrderBy(r => r.StoredFrom)
            .ThenBy(r => r.Firearm!.SerialNumber)
            .Select(StorageRegisterRow.QueryProjection)
            .ToListAsync(cancellationToken);

        // A register silently missing mandatory ID numbers is worse than a failed export, so a
        // decryption failure intentionally propagates and fails the whole request.
        var decrypted = rows
            .Select(r => r.OwnerIdNumberCiphertext is null
                ? r
                : r with { OwnerIdNumber = credentialProtector.Unprotect(r.OwnerIdNumberCiphertext) })
            .ToList();

        var company = await db.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company is null)
        {
            return Error.NotFound(ErrorCodes.CompanyNotFound, "Company not found.");
        }

        var generatedBy = currentUserService.User.Email ?? "unknown";

        var content = query.Format == RegisterExportFormat.Csv
            ? BuildCsv(query.Kind, decrypted)
            : pdfRenderer.Render(RegisterDocumentFactory.Create(
                query.Kind, decrypted, company, query.From, query.To, DateTime.UtcNow, generatedBy));

        await WriteAuditLogAsync(query, decrypted.Count, cancellationToken);

        return new RegisterExportResult(
            content,
            query.Format == RegisterExportFormat.Csv ? "text/csv; charset=utf-8" : "application/pdf",
            FileName(query));
    }

    internal static Error? RowCapError(RegisterExportFormat format, int totalCount)
    {
        var isCsv = format == RegisterExportFormat.Csv;
        var maxRows = isCsv ? MaxExportRows : MaxPdfExportRows;

        if (totalCount <= maxRows)
        {
            return null;
        }

        return Error.Validation(
            ErrorCodes.TooManyRows,
            $"The {(isCsv ? "CSV" : "PDF")} register export is limited to {maxRows} rows. Narrow the date range{(isCsv ? string.Empty : " or export CSV for wider ranges")} and try again.");
    }

    private static byte[] BuildCsv(RegisterKind kind, IReadOnlyList<StorageRegisterRow> rows) =>
        kind == RegisterKind.Firearms
            ? FirearmsRegisterCsvBuilder.Build(rows)
            : SafeCustodyRegisterCsvBuilder.Build(rows);

    private static string FileName(ExportStorageRegisterQuery query)
    {
        var kind = query.Kind == RegisterKind.Firearms ? "firearms" : "safe-custody";
        var extension = query.Format == RegisterExportFormat.Csv ? "csv" : "pdf";
        return $"{kind}-register_{query.From:yyyy-MM-dd}_{query.To:yyyy-MM-dd}.{extension}";
    }

    // Reads are invisible to the audit interceptor (it only sees mutations), so compliance
    // exports self-record: who produced which register, over what range, and how many rows.
    // The EF interceptor still stamps Id, CreatedAt, and CompanyId on insert, which is why
    // they are not set here.
    private async Task WriteAuditLogAsync(
        ExportStorageRegisterQuery query, int rowCount, CancellationToken cancellationToken)
    {
        var appUserId = await db.AppUsers
            .Where(u => u.AuthUserId == currentUserService.User.Id)
            .Select(u => (Guid?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        db.AuditLogs.Add(new AuditLog
        {
            EntityType = "Register",
            EntityId = Guid.CreateVersion7(),
            Action = "Exported",
            NewValue = JsonSerializer.Serialize(new
            {
                register = query.Kind.ToString(),
                from = query.From,
                to = query.To,
                format = query.Format.ToString(),
                rowCount,
            }),
            AppUserId = appUserId,
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    public static class ErrorCodes
    {
        public const string InvalidRange = "ExportStorageRegisterQuery.InvalidRange";
        public const string NoCompany = "ExportStorageRegisterQuery.NoCompany";
        public const string TooManyRows = "ExportStorageRegisterQuery.TooManyRows";
        public const string CompanyNotFound = "ExportStorageRegisterQuery.CompanyNotFound";
    }
}
