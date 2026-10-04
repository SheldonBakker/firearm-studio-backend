namespace FirearmStudio.Infrastructure.Options;

public sealed class FileStorageSettings
{
    public const string SectionName = nameof(FileStorageSettings);

    public string Provider { get; init; } = "s3";
    public string BucketName { get; init; } = "";
    public string ServiceUrl { get; init; } = "";
    public string Region { get; init; } = "us-east-1";
    public string AccessKeyId { get; init; } = "";
    public string SecretAccessKey { get; init; } = "";
    public bool ForcePathStyle { get; init; } = true;
}
