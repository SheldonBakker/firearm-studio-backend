using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;
using FirearmStudio.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FirearmStudio.Application.Dashboard.GetDashboardStats;

public sealed class GetDashboardStatsQueryHandler(IApplicationDbContext db)
    : IQueryHandler<GetDashboardStatsQuery, ErrorOr<DashboardStatsResponse>>
{
    public async Task<ErrorOr<DashboardStatsResponse>> Handle(
        GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        var storage = await db.StorageRecords
            .AsNoTracking()
            .Where(s => s.StorageStatus == StorageStatus.Active)
            .GroupBy(_ => 1)
            .Select(g => new { Count = g.Count(), Total = g.Sum(s => s.MonthlyRate) })
            .FirstOrDefaultAsync(cancellationToken);

        var firearmsCount = await db.Firearms
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var invoices = await db.Invoices
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Outstanding = g.Sum(i =>
                    i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue
                        ? i.Total
                        : 0m),
                Overdue = g.Count(i => i.Status == InvoiceStatus.Overdue),
            })
            .FirstOrDefaultAsync(cancellationToken);

        var licences = await db.FirearmLicences
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(g => new
            {
                RenewalDue = g.Count(l => l.Status == LicenceStatus.RenewalDue),
                Expired = g.Count(l => l.Status == LicenceStatus.Expired),
            })
            .FirstOrDefaultAsync(cancellationToken);

        return new DashboardStatsResponse
        {
            ActiveStorageCount = storage?.Count ?? 0,
            TotalMonthlyRate = storage?.Total ?? 0m,
            FirearmsCount = firearmsCount,
            OutstandingAmount = invoices?.Outstanding ?? 0m,
            OverdueCount = invoices?.Overdue ?? 0,
            LicenceAlerts = new LicenceAlertsDto(
                licences?.RenewalDue ?? 0,
                licences?.Expired ?? 0),
        };
    }
}
