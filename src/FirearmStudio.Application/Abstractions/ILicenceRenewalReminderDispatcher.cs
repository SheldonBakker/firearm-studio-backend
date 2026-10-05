namespace FirearmStudio.Application.Abstractions;

public interface ILicenceRenewalReminderDispatcher
{
    Task DispatchAsync(Guid outboxMessageId, string payloadJson, CancellationToken cancellationToken);
}
