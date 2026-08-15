using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;

namespace ABP.Domain.Interfaces;

public interface IAccountTransactionRepository : IGenericRepository<AccountTransaction,Guid>
{
    Task<PagedResult<AccountTransaction>> GetPagedByAccountAsync( Guid accountId,PagedRequest request,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetByOperationIdAsync( Guid operationId,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetMostRecentByAccountAsync(Guid accountId,int count,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetAllByAccountAsync(Guid accountId, CancellationToken cancellationToken = default);

    Task<int> CountByActorTodayAsync( string actorUserId, DateOnly today, CancellationToken cancellationToken = default);

    Task<decimal> SumAmountByActorTodayAsync( string actorUserId, DateOnly today, CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<int> CountByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<int> CountPaymentsAsync(CancellationToken cancellationToken = default);

    Task<int> CountPaymentsByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

}
