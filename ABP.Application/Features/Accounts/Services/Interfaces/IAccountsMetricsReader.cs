
namespace ABP.Application.Features.Accounts.Services.Interfaces
{
    public interface IAccountsMetricsReader
    {

        Task<int> CountActiveAccountsAsync(CancellationToken cancellationToken = default);

        Task<decimal> SumActiveBalancesAsync(CancellationToken cancellationToken = default);
    }
}
