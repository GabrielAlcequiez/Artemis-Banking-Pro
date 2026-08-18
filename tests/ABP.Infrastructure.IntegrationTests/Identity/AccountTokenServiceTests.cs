using System.Security.Cryptography;
using System.Text;
using ABP.Application.Common.Interfaces.Identity;
using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Identity.Entities;
using ABP.Infrastructure.Identity.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public class AccountTokenServiceTests
{
    private const string EmailConfirmationProviderName = "EmailConfirmation";

    [Fact]
    public async Task GenerateAsync_Activation_PersistsHashedTokenAndExpiry()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        var token = await service.GenerateAsync("user-1", AccountTokenPurpose.Activation);

        Assert.False(string.IsNullOrWhiteSpace(token));
        var persisted = Assert.Single(tokenRepository.Tokens);
        Assert.Equal("user-1", persisted.UserId);
        Assert.Equal(AccountTokenPurpose.Activation, persisted.Purpose);
        Assert.Equal(ComputeTokenHash(token), persisted.TokenHash);
        Assert.True(persisted.ExpiresAtUtc > DateTimeOffset.UtcNow);
        Assert.Null(persisted.UsedAtUtc);
    }

    [Fact]
    public async Task GenerateAsync_PasswordReset_PersistsThirtyMinuteExpiry()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        var token = await service.GenerateAsync("user-1", AccountTokenPurpose.PasswordReset);

        var persisted = Assert.Single(tokenRepository.Tokens);
        Assert.Equal(AccountTokenPurpose.PasswordReset, persisted.Purpose);
        Assert.Equal(TimeSpan.FromMinutes(30), persisted.ExpiresAtUtc - persisted.CreatedAtUtc);
    }

    [Fact]
    public async Task ValidateAsync_ValidToken_ReturnsValid()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        var token = await service.GenerateAsync("user-1", AccountTokenPurpose.Activation);

        var result = await service.ValidateAsync("user-1", token, AccountTokenPurpose.Activation);

        Assert.Equal(AccountTokenValidationStatus.Valid, result.Status);
    }

    [Fact]
    public async Task ValidateAsync_UsedToken_ReturnsUsed()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        var now = DateTimeOffset.UtcNow;
        tokenRepository.Seed(new AccountToken(Guid.NewGuid())
        {
            UserId = "user-1",
            Purpose = AccountTokenPurpose.Activation,
            TokenHash = ComputeTokenHash("used-token"),
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddDays(1),
            UsedAtUtc = now
        });
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        var result = await service.ValidateAsync("user-1", "used-token", AccountTokenPurpose.Activation);

        Assert.Equal(AccountTokenValidationStatus.Used, result.Status);
    }

    [Fact]
    public async Task ValidateAsync_ExpiredToken_ReturnsExpired()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        var now = DateTimeOffset.UtcNow;
        tokenRepository.Seed(new AccountToken(Guid.NewGuid())
        {
            UserId = "user-1",
            Purpose = AccountTokenPurpose.Activation,
            TokenHash = ComputeTokenHash("expired-token"),
            CreatedAtUtc = now.AddDays(-2),
            ExpiresAtUtc = now.AddDays(-1)
        });
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        var result = await service.ValidateAsync("user-1", "expired-token", AccountTokenPurpose.Activation);

        Assert.Equal(AccountTokenValidationStatus.Expired, result.Status);
    }

    [Fact]
    public async Task ValidateAsync_UnknownToken_ReturnsNotFound()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        var result = await service.ValidateAsync("user-1", "missing-token", AccountTokenPurpose.Activation);

        Assert.Equal(AccountTokenValidationStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ValidateAsync_TokenForAnotherPurpose_ReturnsInvalid()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        var resetToken = await service.GenerateAsync("user-1", AccountTokenPurpose.PasswordReset);
        tokenRepository.Seed(new AccountToken(Guid.NewGuid())
        {
            UserId = "user-1",
            Purpose = AccountTokenPurpose.Activation,
            TokenHash = ComputeTokenHash(resetToken),
            CreatedAtUtc = DateTimeOffset.UtcNow,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(1)
        });

        var result = await service.ValidateAsync("user-1", resetToken, AccountTokenPurpose.Activation);

        Assert.Equal(AccountTokenValidationStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task TryMarkAsUsedAsync_SecondAttempt_ReturnsFalse()
    {
        var store = new FakeUserStore();
        store.SeedUser(SeedAppUser());
        var tokenRepository = new FakeAccountTokenRepository();
        using var scope = CreateService(store, tokenRepository);
        var service = scope.Service;

        await service.GenerateAsync("user-1", AccountTokenPurpose.Activation);
        var accountToken = Assert.Single(tokenRepository.Tokens);

        var first = await service.TryMarkAsUsedAsync(accountToken.Id);
        var second = await service.TryMarkAsUsedAsync(accountToken.Id);

        Assert.True(first);
        Assert.False(second);
    }

    private static TestScope CreateService(
        FakeUserStore store,
        FakeAccountTokenRepository tokenRepository)
    {
        var identityOptions = new IdentityOptions();
        identityOptions.Tokens.EmailConfirmationTokenProvider = EmailConfirmationProviderName;
        identityOptions.Tokens.PasswordResetTokenProvider = IdentityTokenProviderNames.PasswordReset;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDataProtection();
        var serviceProvider = services.BuildServiceProvider();

        var userManager = CreateUserManager(store, identityOptions, serviceProvider);

        var service = new AccountTokenService(
            userManager,
            Options.Create(identityOptions),
            Options.Create(new DataProtectionTokenProviderOptions()),
            Options.Create(new PasswordResetTokenProviderOptions()),
            TimeProvider.System,
            tokenRepository);

        return new TestScope(service, serviceProvider);
    }

    private static UserManager<AppUser> CreateUserManager(
        IUserStore<AppUser> store,
        IdentityOptions identityOptions,
        IServiceProvider serviceProvider)
    {
        var userManager = new UserManager<AppUser>(
            store,
            Options.Create(identityOptions),
            new PasswordHasher<AppUser>(),
            [new UserValidator<AppUser>()],
            [new PasswordValidator<AppUser>()],
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            serviceProvider,
            NullLogger<UserManager<AppUser>>.Instance);

        var dataProtectionProvider = serviceProvider.GetRequiredService<IDataProtectionProvider>();

        userManager.RegisterTokenProvider(
            EmailConfirmationProviderName,
            new DataProtectorTokenProvider<AppUser>(
                dataProtectionProvider,
                Options.Create(new DataProtectionTokenProviderOptions()),
                NullLogger<DataProtectorTokenProvider<AppUser>>.Instance));

        userManager.RegisterTokenProvider(
            IdentityTokenProviderNames.PasswordReset,
            new PasswordResetTokenProvider<AppUser>(
                dataProtectionProvider,
                Options.Create(new PasswordResetTokenProviderOptions()),
                NullLogger<DataProtectorTokenProvider<AppUser>>.Instance));

        return userManager;
    }

    private static AppUser SeedAppUser() => new()
    {
        Id = "user-1",
        UserName = "cliente",
        NormalizedUserName = "CLIENTE",
        Email = "cliente@test.com",
        EmailConfirmed = true,
        IsActive = true
    };

    private static string ComputeTokenHash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private sealed class TestScope : IDisposable
    {
        public AccountTokenService Service { get; }

        private readonly ServiceProvider _serviceProvider;

        public TestScope(AccountTokenService service, ServiceProvider serviceProvider)
        {
            Service = service;
            _serviceProvider = serviceProvider;
        }

        public void Dispose() => _serviceProvider.Dispose();
    }

    private sealed class FakeUserStore : IUserStore<AppUser>
    {
        private readonly Dictionary<string, AppUser> _usersById = new();
        private readonly Dictionary<string, AppUser> _usersByName = new(StringComparer.OrdinalIgnoreCase);

        public void SeedUser(AppUser user)
        {
            _usersById[user.Id] = user;
            _usersByName[user.NormalizedUserName ?? user.UserName ?? string.Empty] = user;
        }

        public Task<AppUser?> FindByIdAsync(string userId, CancellationToken cancellationToken) =>
            Task.FromResult(_usersById.GetValueOrDefault(userId));

        public Task<AppUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) =>
            Task.FromResult(_usersByName.GetValueOrDefault(normalizedUserName));

        public Task<string> GetUserIdAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.Id);

        public Task<string?> GetUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.UserName);

        public Task SetUserNameAsync(AppUser user, string? userName, CancellationToken cancellationToken)
        {
            user.UserName = userName;
            return Task.CompletedTask;
        }

        public Task<string?> GetNormalizedUserNameAsync(AppUser user, CancellationToken cancellationToken) =>
            Task.FromResult(user.NormalizedUserName);

        public Task SetNormalizedUserNameAsync(AppUser user, string? normalizedName, CancellationToken cancellationToken)
        {
            user.NormalizedUserName = normalizedName;
            return Task.CompletedTask;
        }

        public Task<IdentityResult> CreateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _usersById[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> UpdateAsync(AppUser user, CancellationToken cancellationToken)
        {
            _usersById[user.Id] = user;
            return Task.FromResult(IdentityResult.Success);
        }

        public Task<IdentityResult> DeleteAsync(AppUser user, CancellationToken cancellationToken)
        {
            _usersById.Remove(user.Id);
            return Task.FromResult(IdentityResult.Success);
        }

        public void Dispose()
        {
        }
    }

    private sealed class FakeAccountTokenRepository : IAccountTokenRepository
    {
        public List<AccountToken> Tokens { get; } = [];

        public void Seed(AccountToken accountToken) => Tokens.Add(accountToken);

        public Task<AccountToken> AddAsync(AccountToken accountToken, CancellationToken cancellationToken = default)
        {
            Tokens.Add(accountToken);
            return Task.FromResult(accountToken);
        }

        public Task<AccountToken?> ExistsAsync(string userId, AccountTokenPurpose purpose, string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tokens.FirstOrDefault(token =>
                token.UserId == userId &&
                token.Purpose == purpose &&
                token.TokenHash == tokenHash));

        public Task<AccountToken?> FindByTokenHashAsync(AccountTokenPurpose purpose, string tokenHash, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tokens
                .Where(token => token.Purpose == purpose && token.TokenHash == tokenHash)
                .OrderByDescending(token => token.CreatedAtUtc)
                .FirstOrDefault());

        public Task<int> MarkAsUsedAsync(Guid accountTokenId, DateTimeOffset usedAtUtc, CancellationToken cancellationToken = default)
        {
            var token = Tokens.FirstOrDefault(candidate =>
                candidate.Id == accountTokenId && candidate.UsedAtUtc == null);
            if (token is null)
            {
                return Task.FromResult(0);
            }

            token.UsedAtUtc = usedAtUtc;
            return Task.FromResult(1);
        }
    }
}