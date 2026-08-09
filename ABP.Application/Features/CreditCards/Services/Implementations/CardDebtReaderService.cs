using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.CreditCards.Services.Implementations;

public sealed class CardDebtReaderService(ICreditCardRepository repository) : ICardDebtReaderService
{
    public Task<decimal> GetActiveCardDebtByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Task.FromResult(0m);
        }

        return repository.GetActiveDebtByClientIdAsync(clientId, cancellationToken);
    }
}
