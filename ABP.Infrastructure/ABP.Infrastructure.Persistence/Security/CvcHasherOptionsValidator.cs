using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.Persistence.Security;

public sealed class CvcHasherOptionsValidator : IValidateOptions<CvcHasherOptions>
{
    private const int MinimumSecretLength = 32;

    public ValidateOptionsResult Validate(string? name, CvcHasherOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SecretBase64))
        {
            return ValidateOptionsResult.Fail(
                $"{CvcHasherOptions.SectionName}:SecretBase64 is required.");
        }

        byte[] decodedSecret;

        try
        {
            decodedSecret = Convert.FromBase64String(options.SecretBase64);
        }
        catch (FormatException)
        {
            return ValidateOptionsResult.Fail(
                $"{CvcHasherOptions.SectionName}:SecretBase64 must be valid Base64.");
        }

        return decodedSecret.Length >= MinimumSecretLength
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(
                $"{CvcHasherOptions.SectionName}:SecretBase64 must contain at least 32 bytes.");
    }
}
