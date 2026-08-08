using System.Security.Cryptography;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Infrastructure.Persistence.Security;
using Microsoft.Extensions.Options;

namespace ABP.Infrastructure.IntegrationTests.CreditCards;

public sealed class CvcServiceTests
{
    [Fact]
    public void Generate_returns_exactly_three_digits_that_can_be_hashed_and_verified()
    {
        var service = CreateService();

        var cvc = service.Generate();
        var hash = service.Hash(cvc);

        Assert.Equal(3, cvc.Length);
        Assert.All(cvc, character => Assert.InRange(character, '0', '9'));
        Assert.True(service.Verify(cvc, hash));
    }

    [Fact]
    public void Hash_and_verify_use_a_keyed_sha256_digest()
    {
        var service = CreateService();

        var hash = service.Hash("123");

        Assert.Equal(64, hash.Length);
        Assert.True(service.Verify("123", hash));
        Assert.False(service.Verify("124", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("12")]
    [InlineData("1234")]
    [InlineData("12a")]
    public void Verify_returns_false_for_invalid_cvc(string cvc)
    {
        var service = CreateService();

        Assert.False(service.Verify(cvc, new string('A', 64)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("AA")]
    public void Verify_returns_false_for_invalid_hash(string hash)
    {
        var service = CreateService();

        Assert.False(service.Verify("123", hash));
    }

    [Fact]
    public void Hash_rejects_a_cvc_that_is_not_three_digits()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.Hash("12"));
    }

    [Fact]
    public void Constructor_rejects_a_secret_shorter_than_thirty_two_bytes()
    {
        var shortSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(31));

        Assert.Throws<InvalidOperationException>(() =>
            new CvcService(Options.Create(new CvcHasherOptions
            {
                SecretBase64 = shortSecret
            })));
    }

    [Fact]
    public void Constructor_rejects_an_invalid_base64_secret()
    {
        Assert.Throws<InvalidOperationException>(() =>
            new CvcService(Options.Create(new CvcHasherOptions
            {
                SecretBase64 = "not-base64"
            })));
    }

    private static ICvcService CreateService() =>
        new CvcService(Options.Create(new CvcHasherOptions
        {
            SecretBase64 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        }));
}
