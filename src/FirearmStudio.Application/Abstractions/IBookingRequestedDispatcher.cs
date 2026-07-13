namespace FirearmStudio.Application.Abstractions;

public interface IBookingRequestedDispatcher
{
    Task DispatchAsync(string payloadJson, CancellationToken cancellationToken);
}
