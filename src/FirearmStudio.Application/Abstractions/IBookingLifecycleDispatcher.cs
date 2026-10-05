namespace FirearmStudio.Application.Abstractions;

public interface IBookingLifecycleDispatcher
{
    Task DispatchAsync(Guid outboxMessageId, string messageType, string payloadJson, CancellationToken cancellationToken);
}
