using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Temporary
{
    // TEMPORAL - Implementación provisional hasta que P2 termine la suya.
    // Al entregar P2: eliminar esta carpeta y cambiar los registros DI en ServicesRegistration.cs.
    public sealed class SavingsAccountRepository : ISavingsAccountRepository
    {
        private readonly AppDbContext _context;

        public SavingsAccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<SavingsAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return _context.SavingsAccounts
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            return _context.SavingsAccounts
                .FirstOrDefaultAsync(
                    x => x.OwnerUserId == ownerUserId
                         && x.Type == SavingsAccountType.Principal
                         && x.Status == SavingsAccountStatus.Active,
                    cancellationToken);
        }

        public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<SavingsAccount>> GetPagedAsync(PagedRequest request, string? ownerIdentification = null,
            SavingsAccountStatus? status = null, SavingsAccountType? type = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AddAsync(SavingsAccount account, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Update(SavingsAccount account)
        {
            _context.SavingsAccounts.Update(account);
        }
    }
}
