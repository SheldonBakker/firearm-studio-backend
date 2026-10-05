namespace FirearmStudio.Application.Abstractions;

public interface IBookingRequestedDispatcher
{
    Task DispatchAsync(Guid outboxMessageId, string payloadJson, CancellationToken cancellationToken);
}
