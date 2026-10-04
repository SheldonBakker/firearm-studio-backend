using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Infrastructure.Options;

namespace FirearmStudio.Infrastructure.Services;

public sealed class S3FileStorage(FileStorageSettings settings) : IFileStorage
{
    private readonly AmazonS3Client _client = new(
        new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey),
        new AmazonS3Config
        {
            ServiceURL = settings.ServiceUrl,
            ForcePathStyle = settings.ForcePathStyle,
            AuthenticationRegion = settings.Region,
            Timeout = TimeSpan.FromSeconds(10),
        });

    private readonly Protocol _protocol =
        Uri.TryCreate(settings.ServiceUrl, UriKind.Absolute, out var serviceUri) && serviceUri.Scheme == Uri.UriSchemeHttp
            ? Protocol.HTTP
            : Protocol.HTTPS;

    public async Task UploadAsync(string key, Stream content, string contentType, CancellationToken ct)
    {
        try
        {
            await _client.PutObjectAsync(
                new PutObjectRequest
                {
                    BucketName = settings.BucketName,
                    Key = key,
                    InputStream = content,
                    ContentType = contentType,
                    AutoCloseStream = false,
                },
                ct);
        }
        catch (Exception ex) when (
            ex is AmazonS3Exception or HttpRequestException or TaskCanceledException or OperationCanceledException
            && !ct.IsCancellationRequested)
        {
            throw new FileStorageException("Image upload failed.", ex);
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct)
    {
        try
        {
            await _client.DeleteObjectAsync(
                new DeleteObjectRequest
                {
                    BucketName = settings.BucketName,
                    Key = key,
                },
                ct);
        }
        catch (Exception ex) when (
            ex is AmazonS3Exception or HttpRequestException or TaskCanceledException or OperationCanceledException
            && !ct.IsCancellationRequested)
        {
            throw new FileStorageException("Image delete failed.", ex);
        }
    }

    public string GetPresignedReadUrl(string key, TimeSpan lifetime) =>
        _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Protocol = _protocol,
            Expires = DateTime.UtcNow.Add(lifetime),
        });
}
