namespace FirearmStudio.Application.Dashboard;

public sealed record LicenceAlertsDto(int RenewalDue, int Expired);

public sealed record DashboardStatsResponse
{
    public required int ActiveStorageCount { get; init; }
    public required decimal TotalMonthlyRate { get; init; }
    public required int FirearmsCount { get; init; }
    public required decimal OutstandingAmount { get; init; }
    public required int OverdueCount { get; init; }
    public required LicenceAlertsDto LicenceAlerts { get; init; }
}
