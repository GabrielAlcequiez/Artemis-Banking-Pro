using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;

namespace ABP.Domain.Interfaces;

public interface ISavingsAccountRepository : IGenericRepository<SavingsAccount, Guid>
{

    Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<PagedResult<SavingsAccount>> GetPagedAsync(PagedRequest request,string? ownerIdentification = null,Domain.Enums.SavingsAccountStatus? status = null,
        Domain.Enums.SavingsAccountType? type = null,
        CancellationToken cancellationToken = default);

  
}
