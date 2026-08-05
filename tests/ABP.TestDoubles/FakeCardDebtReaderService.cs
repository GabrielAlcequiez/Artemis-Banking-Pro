using ABP.Application.Features.CreditCards.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakeCardDebtReaderService : ICardDebtReaderService
    {
        private readonly Dictionary<string, decimal> _clientDebts = new(StringComparer.OrdinalIgnoreCase);

        public decimal DefaultDebt { get; set; } = 0m;

        public void SetDebtForClient(string clientId, decimal debt)
        {
            _clientDebts[clientId] = debt;
        }

        public Task<decimal> GetActiveCardDebtByClientIdAsync(
            string clientId,
            CancellationToken cancellationToken = default)
        {
            if (_clientDebts.TryGetValue(clientId, out var debt))
            {
                return Task.FromResult(debt);
            }

            return Task.FromResult(DefaultDebt);
        }
    }
}
