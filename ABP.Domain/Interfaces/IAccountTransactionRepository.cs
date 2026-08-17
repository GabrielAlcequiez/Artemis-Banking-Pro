using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;

namespace ABP.Domain.Interfaces;

public interface IAccountTransactionRepository : IGenericRepository<AccountTransaction,Guid>
{
    Task<PagedResult<AccountTransaction>> GetPagedByAccountAsync( Guid accountId,PagedRequest request,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetByOperationIdAsync( Guid operationId,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetMostRecentByAccountAsync(Guid accountId,int count,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetAllByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<int> CountByActorTodayAsync( string actorUserId, DateOnly today, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts today's Approved transactions for an actor, restricted to the given operation types.
    /// Pass <paramref name="direction"/> for two-leg operation types (transfers write one Debit
    /// and one Credit row per operation) to avoid counting the same operation twice.
    /// </summary>
    Task<int> CountByActorAndTypesTodayAsync(
        string actorUserId,
        DateOnly today,
        IReadOnlyCollection<FinancialOperationType> types,
        TransactionDirection? direction = null,
        CancellationToken cancellationToken = default);

    Task<decimal> SumAmountByActorTodayAsync( string actorUserId, DateOnly today, CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<int> CountPaymentsAsync(CancellationToken cancellationToken = default);

    Task<int> CountPaymentsByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

}
