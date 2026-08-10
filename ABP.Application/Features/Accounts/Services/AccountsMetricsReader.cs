using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Accounts.Services
{
    public sealed class AccountsMetricsReader : IAccountsMetricsReader
    {
        private readonly ISavingsAccountRepository _accounts;

        public AccountsMetricsReader(ISavingsAccountRepository accounts)
        {
            _accounts = accounts;
        }

        public async Task<int> CountActiveAccountsAsync(CancellationToken cancellationToken = default)
        {
            var page = await _accounts.GetPagedAsync(
                new PagedRequest(1, int.MaxValue), status: SavingsAccountStatus.Active,
                cancellationToken: cancellationToken);

            return page.TotalRecords;
        }

        public async Task<decimal> SumActiveBalancesAsync(CancellationToken cancellationToken = default)
        {
            var page = await _accounts.GetPagedAsync(
                new PagedRequest(1, int.MaxValue), status: SavingsAccountStatus.Active,
                cancellationToken: cancellationToken);

            return page.Data.Sum(a => a.Balance);
        }
    }
}
