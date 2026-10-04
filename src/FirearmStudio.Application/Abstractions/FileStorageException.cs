namespace FirearmStudio.Application.Abstractions;

public sealed class FileStorageException : Exception
{
    public FileStorageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
