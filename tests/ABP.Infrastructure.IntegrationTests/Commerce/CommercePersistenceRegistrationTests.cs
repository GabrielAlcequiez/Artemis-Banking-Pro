using ABP.Application.Common.Interfaces.Services;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ABP.Infrastructure.IntegrationTests.Commerces;

public sealed class CommercePersistenceRegistrationTests
{
    [Fact]
    public void Persistence_registers_the_commerce_repository()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    $"Server={TestDatabase.ResolveServer()};Database=ABP_CommerceRegistrationTests;Trusted_Connection=True;",
                ["Security:Cvc:SecretBase64"] =
                    Convert.ToBase64String(
                        System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            })
            .Build();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton<IClock>(new StubClock());
        services.AddSingleton<ICurrentUserService>(new StubCurrentUser());
        services.AddInfrastructurePersistence(configuration);

        using var provider = services.BuildServiceProvider();

        Assert.IsType<CommerceRepository>(
            provider.GetRequiredService<ICommerceRepository>());
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
