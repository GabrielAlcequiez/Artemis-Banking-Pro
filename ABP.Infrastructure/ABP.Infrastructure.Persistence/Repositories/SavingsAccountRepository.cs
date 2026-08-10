using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class SavingsAccountRepository : GenericRepository<SavingsAccount, Guid>, ISavingsAccountRepository
    {
        public SavingsAccountRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<SavingsAccount?> GetByAccountNumberAsync(
            string accountNumber, CancellationToken cancellationToken = default)
        {
            return await Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);
        }

        public async Task<SavingsAccount?> GetPrincipalAccountAsync(
            string ownerUserId, CancellationToken cancellationToken = default)
        {
            return await Entities
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.OwnerUserId == ownerUserId && a.Type == SavingsAccountType.Principal,
                    cancellationToken);
        }

        public async Task<bool> AccountNumberExistsAsync(
            string accountNumber, CancellationToken cancellationToken = default)
        {
            return await Entities.AnyAsync(a => a.AccountNumber == accountNumber, cancellationToken);
        }

        public async Task<PagedResult<SavingsAccount>> GetPagedAsync(
            PagedRequest request,
            string? ownerIdentification = null,
            SavingsAccountStatus? status = null,
            SavingsAccountType? type = null,
            CancellationToken cancellationToken = default)
        {
            var query = Entities.AsNoTracking();

            if (status is not null)
            {
                query = query.Where(a => a.Status == status);
            }

            if (type is not null)
            {
                query = query.Where(a => a.Type == type);
            }

            if (!string.IsNullOrWhiteSpace(ownerIdentification))
            {
                var ownerUserIds = _context.Set<User>()
                    .Where(u => u.Identification == ownerIdentification)
                    .Select(u => u.Id);

                query = query.Where(a => ownerUserIds.Contains(a.OwnerUserId));
            }

            var totalRecords = await query.CountAsync(cancellationToken);

            var data = await query
                .OrderBy(a => a.AccountNumber)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<SavingsAccount>(data, request.Page, request.PageSize, totalRecords);
        }
    }
}
