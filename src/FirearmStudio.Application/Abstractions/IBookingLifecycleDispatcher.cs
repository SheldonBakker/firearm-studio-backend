namespace FirearmStudio.Application.Abstractions;

public interface IBookingLifecycleDispatcher
{
    Task DispatchAsync(string messageType, string payloadJson, CancellationToken cancellationToken);
}
