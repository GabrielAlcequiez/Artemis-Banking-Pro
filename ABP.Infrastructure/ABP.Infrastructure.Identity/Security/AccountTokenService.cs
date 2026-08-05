using System.Security.Cryptography;
using System.Text;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.Identity.Security;

public sealed class AccountTokenService : IAccountTokenService
{
    private readonly IdentityContext identityContext;
    private readonly UserManager<AppUser> userManager;
    private readonly IdentityOptions identityOptions;
    private readonly DataProtectionTokenProviderOptions activationTokenOptions;
    private readonly PasswordResetTokenProviderOptions passwordResetTokenOptions;
    private readonly TimeProvider timeProvider;

    public AccountTokenService(
        IdentityContext identityContext,
        UserManager<AppUser> userManager,
        IOptions<IdentityOptions> identityOptions,
        IOptions<DataProtectionTokenProviderOptions> activationTokenOptions,
        IOptions<PasswordResetTokenProviderOptions> passwordResetTokenOptions,
        TimeProvider timeProvider)
    {
        this.identityContext = identityContext;
        this.userManager = userManager;
        this.identityOptions = identityOptions.Value;
        this.activationTokenOptions = activationTokenOptions.Value;
        this.passwordResetTokenOptions = passwordResetTokenOptions.Value;
        this.timeProvider = timeProvider;
    }

    public async Task<string> GenerateAsync(
        string userId,
        AccountTokenPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"Identity user '{userId}' was not found.");

        var token = purpose switch
        {
            AccountTokenPurpose.Activation =>
                await userManager.GenerateEmailConfirmationTokenAsync(user),
            AccountTokenPurpose.PasswordReset =>
                await userManager.GeneratePasswordResetTokenAsync(user),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
        };

        var now = timeProvider.GetUtcNow();
        var tokenLifespan = purpose == AccountTokenPurpose.PasswordReset
            ? passwordResetTokenOptions.TokenLifespan
            : activationTokenOptions.TokenLifespan;

        var accountToken = new AccountToken(Guid.NewGuid())
        {
            UserId = userId,
            Purpose = purpose,
            TokenHash = ComputeTokenHash(token),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.Add(tokenLifespan)
        };

        identityContext.AccountTokens.Add(accountToken);
        await identityContext.SaveChangesAsync(cancellationToken);

        return token;
    }

    public async Task<AccountTokenValidationResult> ValidateAsync(
        string userId,
        string token,
        AccountTokenPurpose purpose,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = ComputeTokenHash(token);

        var accountToken = await identityContext.AccountTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.UserId == userId &&
                    candidate.Purpose == purpose &&
                    candidate.TokenHash == tokenHash,
                cancellationToken);

        if (accountToken is null)
        {
            return new AccountTokenValidationResult(AccountTokenValidationStatus.NotFound);
        }

        if (accountToken.UsedAtUtc.HasValue)
        {
            return new AccountTokenValidationResult(
                AccountTokenValidationStatus.Used,
                accountToken.Id);
        }

        if (accountToken.ExpiresAtUtc <= timeProvider.GetUtcNow())
        {
            return new AccountTokenValidationResult(
                AccountTokenValidationStatus.Expired,
                accountToken.Id);
        }

        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return new AccountTokenValidationResult(
                AccountTokenValidationStatus.Invalid,
                accountToken.Id);
        }

        var (provider, identityPurpose) = purpose switch
        {
            AccountTokenPurpose.Activation => (
                identityOptions.Tokens.EmailConfirmationTokenProvider,
                UserManager<AppUser>.ConfirmEmailTokenPurpose),
            AccountTokenPurpose.PasswordReset => (
                identityOptions.Tokens.PasswordResetTokenProvider,
                UserManager<AppUser>.ResetPasswordTokenPurpose),
            _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, null)
        };

        var isValid = await userManager.VerifyUserTokenAsync(
            user,
            provider,
            identityPurpose,
            token);

        return new AccountTokenValidationResult(
            isValid
                ? AccountTokenValidationStatus.Valid
                : AccountTokenValidationStatus.Invalid,
            accountToken.Id);
    }

    public async Task<bool> TryMarkAsUsedAsync(
        Guid accountTokenId,
        CancellationToken cancellationToken = default)
    {
        var usedAtUtc = timeProvider.GetUtcNow();

        var affectedRows = await identityContext.AccountTokens
            .Where(token => token.Id == accountTokenId && token.UsedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    token => token.UsedAtUtc,
                    usedAtUtc),
                cancellationToken);

        return affectedRows == 1;
    }

    private static string ComputeTokenHash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
