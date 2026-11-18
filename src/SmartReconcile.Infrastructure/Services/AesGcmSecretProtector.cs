using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using SmartReconcile.Application.Abstractions;

namespace SmartReconcile.Infrastructure.Services;

public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const int KeySize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[] key;

    public AesGcmSecretProtector(IConfiguration configuration)
        : this(ReadKey(configuration))
    {
    }

    public AesGcmSecretProtector(byte[] key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException("Secret protection key must contain exactly 32 bytes.", nameof(key));

        this.key = key.ToArray();
    }

    public string Protect(string plaintext)
    {
        if (plaintext is null)
            throw new ArgumentNullException(nameof(plaintext));

        var plaintextBytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[1 + NonceSize + TagSize + ciphertext.Length];
        payload[0] = 1;
        Buffer.BlockCopy(nonce, 0, payload, 1, NonceSize);
        Buffer.BlockCopy(tag, 0, payload, 1 + NonceSize, TagSize);
        Buffer.BlockCopy(ciphertext, 0, payload, 1 + NonceSize + TagSize, ciphertext.Length);

        return Convert.ToBase64String(payload);
    }

    public string Unprotect(string protectedValue)
    {
        if (string.IsNullOrWhiteSpace(protectedValue))
            throw new ArgumentException("Protected secret is required.", nameof(protectedValue));

        var payload = Convert.FromBase64String(protectedValue);
        if (payload.Length < 1 + NonceSize + TagSize || payload[0] != 1)
            throw new CryptographicException("Unsupported protected secret format.");

        var nonce = payload.AsSpan(1, NonceSize);
        var tag = payload.AsSpan(1 + NonceSize, TagSize);
        var ciphertext = payload.AsSpan(1 + NonceSize + TagSize);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return System.Text.Encoding.UTF8.GetString(plaintext);
    }

    private static byte[] ReadKey(IConfiguration configuration)
    {
        var value = configuration["Security:EncryptionKey"]
            ?? Environment.GetEnvironmentVariable("SMARTRECONCILE_ENCRYPTION_KEY");

        if (!string.IsNullOrWhiteSpace(value))
        {
            try
            {
                var key = Convert.FromBase64String(value);
                if (key.Length != KeySize)
                    throw new InvalidOperationException("Security:EncryptionKey must be a base64-encoded 32-byte key.");
                return key;
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Security:EncryptionKey must be a base64-encoded 32-byte key.", ex);
            }
        }

        // Render/demo fallback: derive a stable 256-bit key from the Shopify client secret.
        // A dedicated Security:EncryptionKey remains preferred for production and secret rotation.
        var clientSecret = configuration["Shopify:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientSecret))
            throw new InvalidOperationException("Security:EncryptionKey or Shopify:ClientSecret is required for secret protection.");

        return SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(clientSecret));
    }
}
