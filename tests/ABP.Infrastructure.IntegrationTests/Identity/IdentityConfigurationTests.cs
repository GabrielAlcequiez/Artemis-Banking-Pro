using ABP.Application.Common.Interfaces.Identity;
using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Entities;
using ABP.Infrastructure.Identity;
using ABP.Infrastructure.Identity.Context;
using ABP.Infrastructure.Identity.Security;
using ABP.Infrastructure.Identity.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.IntegrationTests.Identity;

public class IdentityConfigurationTests
{
    [Fact]
    public void Registration_applies_identity_and_cookie_requirements()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ABP_IdentityTests;Trusted_Connection=True;"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructureServicesWebApp(configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var identityOptions = serviceProvider
            .GetRequiredService<IOptions<IdentityOptions>>()
            .Value;

        Assert.True(identityOptions.User.RequireUniqueEmail);
        Assert.Equal(8, identityOptions.Password.RequiredLength);
        Assert.Equal(
            IdentityTokenProviderNames.PasswordReset,
            identityOptions.Tokens.PasswordResetTokenProvider);
        Assert.False(identityOptions.SignIn.RequireConfirmedEmail);

        var resetOptions = serviceProvider
            .GetRequiredService<IOptions<PasswordResetTokenProviderOptions>>()
            .Value;

        Assert.Equal(TimeSpan.FromMinutes(30), resetOptions.TokenLifespan);

        var cookieOptions = serviceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get(IdentityConstants.ApplicationScheme);

        Assert.True(cookieOptions.Cookie.HttpOnly);
        Assert.Equal("/Account/Login", cookieOptions.LoginPath.Value);
        Assert.Equal("/Account/AccessDenied", cookieOptions.AccessDeniedPath.Value);

        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(IAccountTokenService) &&
                descriptor.ImplementationType == typeof(AccountTokenService) &&
                descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(
            services,
            descriptor =>
                descriptor.ServiceType == typeof(ICommerceUserInactivationService) &&
                descriptor.ImplementationType == typeof(CommerceUserInactivationService) &&
                descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void Identity_model_contains_persisted_account_tokens()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseSqlServer(
                "Server=(localdb)\\mssqllocaldb;Database=ABP_IdentityTests;Trusted_Connection=True;")
            .Options;

        using var context = new IdentityContext(options);

        var entityType = context.Model.FindEntityType(typeof(AccountToken));

        Assert.NotNull(entityType);
        Assert.Equal("AccountTokens", entityType.GetTableName());
        Assert.Contains(
            entityType.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Single().Name == nameof(AccountToken.TokenHash));
    }
}
