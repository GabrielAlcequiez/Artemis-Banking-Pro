using ABP.Application.Common.Services.Interfaces;

namespace ABP.TestDoubles;

public sealed class FakeCustomerDebtService : ICustomerDebtService
{
    private readonly Dictionary<string, decimal> _clientDebts =
        new(StringComparer.OrdinalIgnoreCase);

    public decimal DefaultDebt { get; set; }

    public decimal AverageDebt { get; set; }

    public void SetDebtForClient(string clientId, decimal debt)
    {
        _clientDebts[clientId] = debt;
    }

    public Task<decimal> GetTotalDebtAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _clientDebts.GetValueOrDefault(clientId, DefaultDebt));
    }

    public Task<decimal> GetAverageActiveClientDebtAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(AverageDebt);
    }

    public Task<IReadOnlyDictionary<string, decimal>> GetTotalDebtsAsync(
        IReadOnlyCollection<string> clientIds,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyDictionary<string, decimal> result = clientIds
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                clientId => clientId,
                clientId => _clientDebts.GetValueOrDefault(clientId, DefaultDebt),
                StringComparer.OrdinalIgnoreCase);

        return Task.FromResult(result);
    }
}
