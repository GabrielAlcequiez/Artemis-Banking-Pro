using System.Data;
using ABP.Application.Common.Interfaces.Persistence;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Transactions;

public sealed class EfFinancialTransaction(AppDbContext context)
    : IFinancialTransaction
{
    public async Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) =>
        await ExecuteAsync(
            IsolationLevel.ReadCommitted,
            operation,
            cancellationToken);

    public async Task<TResult> ExecuteAsync<TResult>(
        IsolationLevel isolationLevel,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!context.Database.IsRelational() ||
            context.Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database
                .BeginTransactionAsync(isolationLevel, cancellationToken);

            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                context.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
