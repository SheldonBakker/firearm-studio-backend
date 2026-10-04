using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Infrastructure.Options;
using FirearmStudio.Infrastructure.Services;
using Testcontainers.Minio;
using Xunit;

namespace FirearmStudio.Infrastructure.Tests.Integration;

public sealed class S3FileStorageTests
{
    [Fact]
    public async Task UploadAsync_wraps_an_unreachable_endpoint_in_FileStorageException()
    {
        var settings = new FileStorageSettings
        {
            Provider = "s3",
            BucketName = "product-images",
            ServiceUrl = "http://127.0.0.1:1",
            Region = "us-east-1",
            AccessKeyId = "key",
            SecretAccessKey = "secret",
            ForcePathStyle = true,
        };

        var storage = new S3FileStorage(settings);
        using var content = new MemoryStream([0x01, 0x02, 0x03]);

        await Assert.ThrowsAsync<FileStorageException>(
            () => storage.UploadAsync("companies/a/products/b/x.jpg", content, "image/jpeg", CancellationToken.None));
    }

    [Fact]
    [Trait("Category", "Docker")]
    public async Task Upload_presign_read_and_delete_round_trip_against_minio()
    {
        await using var minio = new MinioBuilder("cgr.dev/chainguard/minio:latest").Build();
        await minio.StartAsync();

        var settings = new FileStorageSettings
        {
            Provider = "s3",
            BucketName = "product-images",
            ServiceUrl = minio.GetConnectionString(),
            Region = "us-east-1",
            AccessKeyId = minio.GetAccessKey(),
            SecretAccessKey = minio.GetSecretKey(),
            ForcePathStyle = true,
        };

        var s3Config = new AmazonS3Config
        {
            ServiceURL = settings.ServiceUrl,
            ForcePathStyle = true,
            AuthenticationRegion = settings.Region,
        };
        using (var admin = new AmazonS3Client(
            new BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey), s3Config))
        {
            await admin.PutBucketAsync(new PutBucketRequest { BucketName = settings.BucketName });
        }

        var storage = new S3FileStorage(settings);
        byte[] payload = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03, 0x04];
        const string key = "companies/a/products/b/image.jpg";

        using (var upload = new MemoryStream(payload))
        {
            await storage.UploadAsync(key, upload, "image/jpeg", CancellationToken.None);
        }

        var url = storage.GetPresignedReadUrl(key, TimeSpan.FromMinutes(60));

        using var http = new HttpClient();
        var fetched = await http.GetByteArrayAsync(url);
        Assert.Equal(payload, fetched);

        await storage.DeleteAsync(key, CancellationToken.None);

        var afterDelete = await http.GetAsync(url);
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }
}
