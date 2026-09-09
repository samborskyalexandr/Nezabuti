using System.Security.Cryptography;
using System.Text;

namespace Nezabuti.Api.Services;

public interface ISecretEncryptionService
{
    string Encrypt(string plaintext);
    string? Decrypt(string? ciphertext);
    string Mask(string? plaintextOrCipher, int visibleTail = 4);
}

/// <summary>
/// AES-256-GCM encryption for secrets (Telegram bot token, etc.).
/// Key from SECRET_ENCRYPTION_KEY (32+ chars) or derived from JWT_SECRET.
/// </summary>
public sealed class SecretEncryptionService : ISecretEncryptionService
{
    private readonly byte[] _key;

    public SecretEncryptionService(IConfiguration configuration)
    {
        var raw = configuration["SECRET_ENCRYPTION_KEY"]
            ?? configuration["JWT_SECRET"]
            ?? configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("SECRET_ENCRYPTION_KEY or JWT_SECRET is required for secret encryption.");

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
    }

    public string Encrypt(string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return string.Empty;
        }

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plainBytes.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plainBytes, cipher, tag);

        var payload = new byte[nonce.Length + tag.Length + cipher.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length, tag.Length);
        Buffer.BlockCopy(cipher, 0, payload, nonce.Length + tag.Length, cipher.Length);
        return "enc:" + Convert.ToBase64String(payload);
    }

    public string? Decrypt(string? ciphertext)
    {
        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            return null;
        }

        if (!ciphertext.StartsWith("enc:", StringComparison.Ordinal))
        {
            // Legacy plaintext (migration) — return as-is.
            return ciphertext;
        }

        try
        {
            var payload = Convert.FromBase64String(ciphertext["enc:".Length..]);
            if (payload.Length < 12 + 16)
            {
                return null;
            }

            var nonce = payload.AsSpan(0, 12);
            var tag = payload.AsSpan(12, 16);
            var cipher = payload.AsSpan(28);
            var plain = new byte[cipher.Length];
            using var aes = new AesGcm(_key, 16);
            aes.Decrypt(nonce, cipher, tag, plain);
            return Encoding.UTF8.GetString(plain);
        }
        catch
        {
            return null;
        }
    }

    public string Mask(string? plaintextOrCipher, int visibleTail = 4)
    {
        var value = Decrypt(plaintextOrCipher) ?? plaintextOrCipher;
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (value.Length <= visibleTail)
        {
            return new string('•', value.Length);
        }

        return new string('•', Math.Min(12, value.Length - visibleTail)) + value[^visibleTail..];
    }
}
