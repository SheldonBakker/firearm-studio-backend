namespace FirearmStudio.Application.Licences.Reminders;

internal sealed record LicenceRenewalReminderPayload(
    string Email,
    string? CustomerName,
    string LicenceNumber,
    DateOnly ExpiresOn,
    int DaysUntilExpiry,
    string Tier,
    string FirearmMake,
    string? FirearmModel,
    string SerialNumber,
    Guid CompanyId,
    string CompanyName);
