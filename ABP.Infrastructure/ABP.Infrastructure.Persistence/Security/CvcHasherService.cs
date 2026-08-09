using System.Security.Cryptography;
using System.Text;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.Persistence.Security;

public sealed class CvcHasherService : ICvcHasherService
{
    private const int MinimumSecretLength = 32;
    private const int CvcLength = 3;
    private const int HashLength = 32;
    private readonly byte[] secretKey;

    public CvcHasherService(IOptions<CvcHasherOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var secretBase64 = options.Value.SecretBase64;
        byte[] decodedSecret;

        try
        {
            decodedSecret = Convert.FromBase64String(secretBase64);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "The CVC HMAC secret must be valid Base64.",
                exception);
        }

        if (decodedSecret.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException(
                "The CVC HMAC secret must contain at least 32 bytes.");
        }

        secretKey = decodedSecret;
    }

    public string Hash(string cvc)
    {
        if (!IsValidCvc(cvc))
        {
            throw new ArgumentException("The CVC must contain exactly three digits.", nameof(cvc));
        }

        using var hmac = new HMACSHA256(secretKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(cvc));

        return Convert.ToHexString(hash);
    }

    public bool Verify(string cvc, string cvcHash)
    {
        if (!IsValidCvc(cvc) || string.IsNullOrWhiteSpace(cvcHash))
        {
            return false;
        }

        byte[] expectedHash;

        try
        {
            expectedHash = Convert.FromHexString(cvcHash);
        }
        catch (FormatException)
        {
            return false;
        }

        if (expectedHash.Length != HashLength)
        {
            return false;
        }

        using var hmac = new HMACSHA256(secretKey);
        var actualHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(cvc));

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static bool IsValidCvc(string? cvc) =>
        cvc is not null &&
        cvc.Length == CvcLength &&
        cvc.All(character => character is >= '0' and <= '9');
}
