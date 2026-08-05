namespace ABP.Application.Interfaces.Services;

public interface ILoansMetricsReader
{
    Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default);
    Task<decimal> SumActiveLoanDebtAsync(CancellationToken cancellationToken = default);
}
