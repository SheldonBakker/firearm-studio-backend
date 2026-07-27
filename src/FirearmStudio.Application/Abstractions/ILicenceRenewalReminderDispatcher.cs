namespace FirearmStudio.Application.Abstractions;

public interface ILicenceRenewalReminderDispatcher
{
    Task DispatchAsync(string payloadJson, CancellationToken cancellationToken);
}
