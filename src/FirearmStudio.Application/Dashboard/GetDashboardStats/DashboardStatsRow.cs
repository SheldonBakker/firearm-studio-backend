namespace FirearmStudio.Application.Dashboard.GetDashboardStats;

// Raw SQL result type for the single-round-trip dashboard stats query.
// Defined here so Infrastructure can reference it via IApplicationDbContext.
public sealed record DashboardStatsRow(
    int ActiveStorageCount,
    decimal TotalMonthlyRate,
    int FirearmsCount,
    decimal OutstandingAmount,
    int OverdueCount,
    int LicenceRenewalDue,
    int LicenceExpired);
