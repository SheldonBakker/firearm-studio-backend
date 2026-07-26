using ErrorOr;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Abstractions.Messaging;

namespace FirearmStudio.Application.Dashboard.GetDashboardStats;

public sealed class GetDashboardStatsQueryHandler(IApplicationDbContext db, ITenantContext tenant)
    : IQueryHandler<GetDashboardStatsQuery, ErrorOr<DashboardStatsResponse>>
{
    public async Task<ErrorOr<DashboardStatsResponse>> Handle(
        GetDashboardStatsQuery query, CancellationToken cancellationToken)
    {
        var companyId = tenant.CompanyId ?? Guid.Empty;
        var row = await db.GetDashboardStatsRowAsync(companyId, cancellationToken);

        return new DashboardStatsResponse
        {
            ActiveStorageCount = row.ActiveStorageCount,
            TotalMonthlyRate = row.TotalMonthlyRate,
            FirearmsCount = row.FirearmsCount,
            OutstandingAmount = row.OutstandingAmount,
            OverdueCount = row.OverdueCount,
            LicenceAlerts = new LicenceAlertsDto(row.LicenceRenewalDue, row.LicenceExpired),
        };
    }
}
