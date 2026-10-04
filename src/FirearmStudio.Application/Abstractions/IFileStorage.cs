namespace FirearmStudio.Application.Abstractions;

public interface IFileStorage
{
    Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct);

    Task DeleteAsync(string key, CancellationToken ct);

    string GetPresignedReadUrl(string key, TimeSpan lifetime);
}
