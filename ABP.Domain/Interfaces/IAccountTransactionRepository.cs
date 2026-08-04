using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;

namespace ABP.Domain.Interfaces;

public interface IAccountTransactionRepository
{
    Task<PagedResult<AccountTransaction>> GetPagedByAccountAsync( Guid accountId,PagedRequest request,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetByOperationIdAsync(Guid operationId,CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<AccountTransaction>> GetMostRecentByAccountAsync(Guid accountId,int count,CancellationToken cancellationToken = default);

    Task AddAsync(AccountTransaction transaction, CancellationToken cancellationToken = default);
}
