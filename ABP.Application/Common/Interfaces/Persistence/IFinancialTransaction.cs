using System.Data;

namespace ABP.Application.Common.Interfaces.Persistence;

public interface IFinancialTransaction
{
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    Task<TResult> ExecuteAsync<TResult>(
        IsolationLevel isolationLevel,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(operation, cancellationToken);
}
