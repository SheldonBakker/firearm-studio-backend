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
        var activeStorageCount = await db.StorageRecords
            .AsNoTracking()
            .Where(s => s.StorageStatus == StorageStatus.Active)
            .CountAsync(cancellationToken);

        var totalMonthlyRate = await db.StorageRecords
            .AsNoTracking()
            .Where(s => s.StorageStatus == StorageStatus.Active)
            .SumAsync(s => s.MonthlyRate, cancellationToken);

        var firearmsCount = await db.Firearms
            .AsNoTracking()
            .CountAsync(cancellationToken);

        var outstandingAmount = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Sent || i.Status == InvoiceStatus.Overdue)
            .SumAsync(i => i.Total, cancellationToken);

        var overdueCount = await db.Invoices
            .AsNoTracking()
            .Where(i => i.Status == InvoiceStatus.Overdue)
            .CountAsync(cancellationToken);

        var renewalDueCount = await db.FirearmLicences
            .AsNoTracking()
            .Where(l => l.Status == LicenceStatus.RenewalDue)
            .CountAsync(cancellationToken);

        var expiredCount = await db.FirearmLicences
            .AsNoTracking()
            .Where(l => l.Status == LicenceStatus.Expired)
            .CountAsync(cancellationToken);

        return new DashboardStatsResponse
        {
            ActiveStorageCount = activeStorageCount,
            TotalMonthlyRate = totalMonthlyRate,
            FirearmsCount = firearmsCount,
            OutstandingAmount = outstandingAmount,
            OverdueCount = overdueCount,
            LicenceAlerts = new LicenceAlertsDto(renewalDueCount, expiredCount),
        };
    }
}
