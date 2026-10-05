namespace FirearmStudio.Application.Abstractions.Email;

public sealed record LicenceRenewalReminderEmail(
    string RecipientEmail, string? CustomerName,
    string LicenceNumber, DateOnly ExpiresOn, int DaysUntilExpiry, string Tier,
    string FirearmMake, string? FirearmModel, string SerialNumber,
    CompanyEmailDetails Company) : EmailMessage;
