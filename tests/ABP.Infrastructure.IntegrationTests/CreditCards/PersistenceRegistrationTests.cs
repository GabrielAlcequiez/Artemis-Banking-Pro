using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence;
using ABP.Infrastructure.Persistence.Repositories;
using ABP.Infrastructure.Persistence.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class PersistenceRegistrationTests
{
    [Fact]
    public void Persistence_registers_card_and_hermes_repositories_and_cvc_service()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\mssqllocaldb;Database=ABP_CreditCardTests;Trusted_Connection=True;",
                ["Security:Cvc:SecretBase64"] =
                    Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            })
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IClock>(new StubClock());
        services.AddSingleton<ICurrentUserService>(new StubCurrentUser());
        services.AddInfrastructurePersistence(configuration);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IStartupValidator>().Validate();

        Assert.IsType<CreditCardRepository>(
            provider.GetRequiredService<ICreditCardRepository>());
        Assert.IsType<HermesTransactionRepository>(
            provider.GetRequiredService<IHermesTransactionRepository>());
        Assert.IsType<CvcService>(
            provider.GetRequiredService<ICvcService>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-base64")]
    public void Persistence_rejects_an_invalid_cvc_secret_during_startup(string? secretBase64)
    {
        using var provider = BuildProvider(secretBase64);

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());
    }

    [Fact]
    public void Persistence_rejects_a_short_cvc_secret_during_startup()
    {
        var shortSecret = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(31));
        using var provider = BuildProvider(shortSecret);

        Assert.Throws<OptionsValidationException>(() =>
            provider.GetRequiredService<IStartupValidator>().Validate());
    }

    private static ServiceProvider BuildProvider(string? secretBase64)
    {
        var values = new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] =
                "Server=(localdb)\\mssqllocaldb;Database=ABP_CreditCardTests;Trusted_Connection=True;"
        };

        if (secretBase64 is not null)
        {
            values["Security:Cvc:SecretBase64"] = secretBase64;
        }

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IClock>(new StubClock());
        services.AddSingleton<ICurrentUserService>(new StubCurrentUser());
        services.AddInfrastructurePersistence(configuration);

        return services.BuildServiceProvider();
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

        public DateTimeOffset Now => UtcNow;

        public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public bool IsAuthenticated => false;

        public string? UserId => null;

        public string? UserName => null;

        public Guid? CommerceId => null;

        public IReadOnlyCollection<string> Roles => [];

        public bool IsInRole(string role) => false;
    }
}
