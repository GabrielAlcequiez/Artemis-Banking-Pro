using ABP.Application.Features.Accounts.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakeAccountsMetricsReader : IAccountsMetricsReader
    {
        public int ActiveAccountsCount { get; set; } = 0;

        public decimal ActiveBalancesSum { get; set; } = 0m;

        public Task<int> CountActiveAccountsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActiveAccountsCount);
        }

        public Task<decimal> SumActiveBalancesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ActiveBalancesSum);
        }
    }
}
