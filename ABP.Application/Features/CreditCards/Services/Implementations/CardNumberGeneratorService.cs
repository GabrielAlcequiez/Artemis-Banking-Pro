using System.Security.Cryptography;
using ABP.Application.Features.CreditCards.Services.Interfaces;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CardNumberGeneratorService : ICardNumberGeneratorService
{
    private const int CardNumberLength = 16;

    public string Generate()
    {
        Span<char> digits = stackalloc char[CardNumberLength];

        for (var index = 0; index < digits.Length; index++)
        {
            digits[index] = (char)('0' + RandomNumberGenerator.GetInt32(0, 10));
        }

        return new string(digits);
    }
}
