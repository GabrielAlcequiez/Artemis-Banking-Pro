using Microsoft.AspNetCore.Identity;

namespace ABP.Infrastructure.Identity.Security;

public sealed class PasswordResetTokenProviderOptions : DataProtectionTokenProviderOptions
{
    public PasswordResetTokenProviderOptions()
    {
        Name = IdentityTokenProviderNames.PasswordReset;
        TokenLifespan = TimeSpan.FromMinutes(30);
    }
}
