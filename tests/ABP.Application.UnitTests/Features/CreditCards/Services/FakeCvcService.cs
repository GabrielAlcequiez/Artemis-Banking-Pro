using ABP.Application.Features.CreditCards.Services.Interfaces;

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

internal sealed class FakeCvcService : ICvcService
{
    public string GeneratedCvc { get; init; } = "123";

    public string HashedCvc { get; init; } = new('A', 64);

    public string? LastHashedCvc { get; private set; }

    public string Generate() => GeneratedCvc;

    public string Hash(string cvc)
    {
        LastHashedCvc = cvc;
        return HashedCvc;
    }

    public bool Verify(string cvc, string cvcHash) =>
        cvc == GeneratedCvc && cvcHash == HashedCvc;
}
