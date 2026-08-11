using ABP.Application.Common.Services.Interfaces;
using ABP.Domain.Interfaces;

namespace ABP.Application.Common.Services.Implementations;

public sealed class CustomerDebtService(
    IUserRepository userRepository,
    ILoanRepository loanRepository,
    ICreditCardRepository creditCardRepository)
    : ICustomerDebtService
{
    public async Task<decimal> GetTotalDebtAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return 0m;
        }

        var loanDebt = await loanRepository.GetActiveDebtByClientIdAsync(
            clientId,
            cancellationToken);
        var cardDebt = await creditCardRepository.GetActiveDebtByClientIdAsync(
            clientId,
            cancellationToken);

        return loanDebt + cardDebt;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetTotalDebtsAsync(
        IReadOnlyCollection<string> clientIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientIds);

        var distinctClientIds = clientIds
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (distinctClientIds.Length == 0)
        {
            return new Dictionary<string, decimal>();
        }

        var loanDebts = await loanRepository.GetActiveDebtByClientIdsAsync(
            distinctClientIds,
            cancellationToken);
        var cardDebts = await creditCardRepository.GetActiveDebtByClientIdsAsync(
            distinctClientIds,
            cancellationToken);

        return distinctClientIds.ToDictionary(
            clientId => clientId,
            clientId => loanDebts.GetValueOrDefault(clientId)
                + cardDebts.GetValueOrDefault(clientId),
            StringComparer.Ordinal);
    }

    public async Task<decimal> GetAverageActiveClientDebtAsync(
        CancellationToken cancellationToken = default)
    {
        var activeClientCount = await userRepository.CountActiveClientsAsync(
            cancellationToken);

        if (activeClientCount == 0)
        {
            return 0m;
        }

        var loanDebt = await loanRepository.GetTotalActiveDebtForActiveClientsAsync(
            cancellationToken);
        var cardDebt = await creditCardRepository.GetTotalActiveDebtForActiveClientsAsync(
            cancellationToken);

        return (loanDebt + cardDebt) / activeClientCount;
    }
}
