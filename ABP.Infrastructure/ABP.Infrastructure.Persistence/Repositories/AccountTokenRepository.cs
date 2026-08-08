using ABP.Domain.Entities;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class AccountTokenRepository : IAccountTokenRepository
    {
        private readonly AppDbContext _context;

        public AccountTokenRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AccountToken?> ExistsAsync(string userId, AccountTokenPurpose purpose, string tokenHash, CancellationToken cancellationToken = default)
        {
            return await _context.AccountTokens
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId && x.Purpose == purpose && x.TokenHash == tokenHash, cancellationToken);
        }

        public Task<int> MarkAsUsedAsync(Guid accountTokenId, DateTimeOffset usedAtUtc, CancellationToken cancellationToken = default)
        {
            return _context.AccountTokens.Where(token => token.Id == accountTokenId && token.UsedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    token => token.UsedAtUtc,
                    usedAtUtc),
                cancellationToken);
        }
    }
}