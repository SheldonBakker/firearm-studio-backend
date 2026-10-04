using FirearmStudio.Application.Abstractions;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class FakeFileStorage : IFileStorage
{
    public List<string> Uploaded { get; } = [];
    public List<string> Deleted { get; } = [];
    public bool FailUploads { get; set; }
    public bool FailDeletes { get; set; }
    public string PresignedBaseUrl { get; set; } = "https://files.test";

    public Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        if (FailUploads)
        {
            throw new FileStorageException("upload failed", new InvalidOperationException());
        }

        Uploaded.Add(key);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string key, CancellationToken ct)
    {
        if (FailDeletes)
        {
            throw new FileStorageException("delete failed", new InvalidOperationException());
        }

        Deleted.Add(key);
        return Task.CompletedTask;
    }

    public string GetPresignedReadUrl(string key, TimeSpan lifetime) => $"{PresignedBaseUrl}/{key}";
}
