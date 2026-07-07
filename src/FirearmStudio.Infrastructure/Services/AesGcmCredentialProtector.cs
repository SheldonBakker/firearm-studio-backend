using System.Security.Cryptography;
using FirearmStudio.Application.Abstractions;
using FirearmStudio.Application.Model.Options;

namespace FirearmStudio.Infrastructure.Services;

public sealed class AesGcmCredentialProtector : ICredentialProtector
{
    private const string Version = "v1";
    private const int KeySizeBytes = 32;
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    private readonly byte[] _key;

    public AesGcmCredentialProtector(CredentialProtectionSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.Key))
        {
            throw new InvalidOperationException(
                $"Missing required configuration '{CredentialProtectionSettings.SectionName}:Key'. " +
                "Set it to a base64-encoded 32-byte key.");
        }

        try
        {
            _key = Convert.FromBase64String(settings.Key);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException(
                $"Configuration '{CredentialProtectionSettings.SectionName}:Key' must be valid base64.", ex);
        }

        if (_key.Length != KeySizeBytes)
        {
            throw new InvalidOperationException(
                $"Configuration '{CredentialProtectionSettings.SectionName}:Key' must decode to {KeySizeBytes} bytes.");
        }
    }

    public string Protect(string value)
    {
        var plaintext = System.Text.Encoding.UTF8.GetBytes(value);
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return string.Join(
            ':',
            Version,
            Convert.ToBase64String(nonce),
            Convert.ToBase64String(tag),
            Convert.ToBase64String(ciphertext));
    }

    public string Unprotect(string protectedValue)
    {
        var parts = protectedValue.Split(':');
        if (parts.Length != 4 || parts[0] != Version)
        {
            throw new InvalidOperationException("Unsupported protected credential format.");
        }

        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return System.Text.Encoding.UTF8.GetString(plaintext);
    }
}
