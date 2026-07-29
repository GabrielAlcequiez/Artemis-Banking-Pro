using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ABP.Infrastructure.Identity.Security;

namespace ABP.Infrastructure.Identity
{
    public class ConfigureIdentityOptions : IConfigureOptions<IdentityOptions>
    {
        public void Configure(IdentityOptions options)
        {
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;

            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@";

            options.SignIn.RequireConfirmedEmail = false;
            options.Tokens.PasswordResetTokenProvider = IdentityTokenProviderNames.PasswordReset;
        }
    }
}
