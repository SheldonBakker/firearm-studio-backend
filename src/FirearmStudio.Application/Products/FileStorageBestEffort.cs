using FirearmStudio.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace FirearmStudio.Application.Products;

public static class FileStorageBestEffort
{
    public static async Task TryDeleteAsync(
        this IFileStorage storage,
        string key,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            await storage.DeleteAsync(key, cancellationToken);
        }
        catch (FileStorageException ex)
        {
            logger.LogWarning(ex, "Failed to delete object {Key}.", key);
        }
    }
}
