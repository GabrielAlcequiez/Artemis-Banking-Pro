using ABP.Domain.Entities.Commerce;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class CommerceRepository(AppDbContext context) : GenericRepository<Commerce, Guid>(context), ICommerceRepository
    {
        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludingCommerceId = null,
            CancellationToken cancellationToken = default)
        {
            return _context.Commerces
                .AsNoTracking()
                .AnyAsync(
                    commerce => commerce.Email == email &&
                        (excludingCommerceId == null || commerce.Id != excludingCommerceId),
                    cancellationToken);
        }

        public Task<bool> RncExistsAsync(
            string rnc,
            Guid? excludingCommerceId = null,
            CancellationToken cancellationToken = default)
        {
            return _context.Commerces
                .AsNoTracking()
                .AnyAsync(
                    commerce => commerce.Rnc == rnc &&
                        (excludingCommerceId == null || commerce.Id != excludingCommerceId),
                    cancellationToken);
        }
    }
}