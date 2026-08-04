namespace ABP.Application.Interfaces.Services;


public interface IAccountsMetricsReader
{
    Task<int> CountActiveAccountsAsync(CancellationToken cancellationToken = default);

    Task<decimal> SumActiveBalancesAsync(CancellationToken cancellationToken = default);
}
