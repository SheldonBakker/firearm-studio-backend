namespace FirearmStudio.Application.Dashboard.GetDashboardStats;

public sealed record DashboardStatsRow(
    int ActiveStorageCount,
    decimal TotalMonthlyRate,
    int FirearmsCount,
    decimal OutstandingAmount,
    int OverdueCount,
    int LicenceRenewalDue,
    int LicenceExpired);
