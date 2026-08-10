using ABP.Application.Features.CreditCards.Services.Interfaces;

namespace ABP.Application.UnitTests.Features.CreditCards.Services;

internal sealed class FakeCardNumberGeneratorService(
    string cardNumber = "0000000000001234") : ICardNumberGeneratorService
{
    public int GenerateCalls { get; private set; }

    public string Generate()
    {
        GenerateCalls++;
        return cardNumber;
    }
}
