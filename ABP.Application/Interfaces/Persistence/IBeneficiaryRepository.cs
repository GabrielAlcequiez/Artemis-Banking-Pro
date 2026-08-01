using ABP.Domain.Entities;

namespace ABP.Application.Interfaces.Persistence;

public interface IBeneficiaryRepository
{
    Task<IReadOnlyCollection<Beneficiary>> GetByOwnerAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task<Beneficiary?> GetAsync(string ownerUserId, Guid beneficiaryAccountId, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string ownerUserId, Guid beneficiaryAccountId, CancellationToken cancellationToken = default);

    Task AddAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default);

    void Remove(Beneficiary beneficiary);
}
