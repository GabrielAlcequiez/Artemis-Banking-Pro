namespace ABP.Infrastructure.Persistence.Security;

public sealed class CvcHasherOptions
{
    public const string SectionName = "Security:Cvc";

    public string SecretBase64 { get; set; } = string.Empty;
}
