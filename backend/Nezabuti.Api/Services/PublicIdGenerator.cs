using System.Security.Cryptography;
using System.Text;

namespace Nezabuti.Api.Services;

public interface IPublicIdGenerator
{
    string Generate();
}

/// <summary>
/// Cryptographically random PublicId (~10 chars), excluding ambiguous 0/O/1/I.
/// </summary>
public sealed class PublicIdGenerator : IPublicIdGenerator
{
    // No 0, O, 1, I
    private const string Alphabet = "23456789ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const int Length = 10;

    public string Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(Length);
        var sb = new StringBuilder(Length);
        for (var i = 0; i < Length; i++)
        {
            sb.Append(Alphabet[bytes[i] % Alphabet.Length]);
        }

        return sb.ToString();
    }
}
