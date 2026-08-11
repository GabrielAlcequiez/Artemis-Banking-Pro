namespace ABP.Application.Common.Interfaces.Persistence;

public interface IFinancialTransaction
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);
}
