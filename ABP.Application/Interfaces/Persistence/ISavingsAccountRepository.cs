using ABP.Application.Common;
using ABP.Domain.Entities;

namespace ABP.Application.Interfaces.Persistence;

public interface ISavingsAccountRepository
{
    Task<SavingsAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default);

    Task<PagedResult<SavingsAccount>> GetPagedAsync(PagedRequest request,string? ownerIdentification = null,Domain.Enums.SavingsAccountStatus? status = null,
        Domain.Enums.SavingsAccountType? type = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(SavingsAccount account, CancellationToken cancellationToken = default);

    void Update(SavingsAccount account);
}
