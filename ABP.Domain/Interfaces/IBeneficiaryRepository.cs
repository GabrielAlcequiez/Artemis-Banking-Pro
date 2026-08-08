using ABP.Domain.Entities.Accounts;

namespace ABP.Domain.Interfaces;

public interface IBeneficiaryRepository : IGenericRepository<Beneficiary, Guid>
{
    Task<IReadOnlyCollection<Beneficiary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task<Beneficiary?> GetAsync(string ownerUserId, Guid beneficiaryAccountId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string ownerUserId, Guid beneficiaryAccountId, CancellationToken cancellationToken = default);

   
}
