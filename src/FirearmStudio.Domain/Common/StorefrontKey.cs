using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace FirearmStudio.Domain.Common;

public static class StorefrontKey
{
    public static string Generate()
    {
        var bytes = new byte[StorefrontKeyConstants.RandomBytes];
        RandomNumberGenerator.Fill(bytes);
        return StorefrontKeyConstants.Prefix + Base64Url.EncodeToString(bytes);
    }

    public static bool Matches(string? stored, string? provided)
    {
        if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(provided))
        {
            return false;
        }

        var storedBytes = Encoding.UTF8.GetBytes(stored);
        var providedBytes = Encoding.UTF8.GetBytes(provided);

        return CryptographicOperations.FixedTimeEquals(storedBytes, providedBytes);
    }

    public static string AuditSuffix(string key) =>
        key[^StorefrontKeyConstants.AuditSuffixLength..];
}
