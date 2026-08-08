using ABP.Domain.Entities.Accounts;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class BeneficiaryRepository : GenericRepository<Beneficiary, Guid>, IBeneficiaryRepository
    {
        public BeneficiaryRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyCollection<Beneficiary>> GetByOwnerAsync(
            string ownerUserId, CancellationToken cancellationToken = default)
        {
            return await Entities.AsNoTracking().Where(b => b.OwnerUserId == ownerUserId)
                .ToListAsync(cancellationToken);
        }

        public async Task<Beneficiary?> GetAsync(
            string ownerUserId, Guid beneficiaryAccountId, CancellationToken cancellationToken = default)
        {
            return await Entities.AsNoTracking().FirstOrDefaultAsync(
                    b => b.OwnerUserId == ownerUserId && b.BeneficiaryAccountId == beneficiaryAccountId,
                    cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            string ownerUserId, Guid beneficiaryAccountId, CancellationToken cancellationToken = default)
        {
            return await Entities.AnyAsync(
                    b => b.OwnerUserId == ownerUserId && b.BeneficiaryAccountId == beneficiaryAccountId,
                    cancellationToken);
        }
    }
}
