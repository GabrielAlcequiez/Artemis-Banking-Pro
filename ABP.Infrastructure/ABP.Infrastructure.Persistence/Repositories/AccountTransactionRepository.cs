using ABP.Domain.Common;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories
{
    public class AccountTransactionRepository : GenericRepository<AccountTransaction, Guid>, IAccountTransactionRepository
    {
        public AccountTransactionRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<PagedResult<AccountTransaction>> GetPagedByAccountAsync(Guid accountId, PagedRequest request, CancellationToken cancellationToken = default)
        {
            var query = Entities.AsNoTracking().Where(t => t.AccountId == accountId);

            var totalRecords = await query.CountAsync(cancellationToken);

            var data = await query
                .OrderByDescending(t => t.CreatedAtUtc)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<AccountTransaction>(data, request.Page, request.PageSize, totalRecords);
        }

        public async Task<IReadOnlyCollection<AccountTransaction>> GetByOperationIdAsync( Guid operationId, CancellationToken cancellationToken = default)
        {
            return await Entities
                .AsNoTracking()
                .Where(t => t.OperationId == operationId)
                .ToListAsync(cancellationToken);
        }



        public async Task<IReadOnlyCollection<AccountTransaction>> GetMostRecentByAccountAsync( Guid accountId, int count, CancellationToken cancellationToken = default)
        {
            return await Entities
                .AsNoTracking()
                .Where(t => t.AccountId == accountId)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Take(count)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountByActorTodayAsync( string actorUserId, DateOnly today, CancellationToken cancellationToken = default)
        {
            return await Entities
                .Where(t =>
                    t.ActorUserId == actorUserId &&
                    t.Status == TransactionStatus.Approved &&
                    DateOnly.FromDateTime(t.CreatedAtUtc.UtcDateTime) == today)
                .CountAsync(cancellationToken);
        }

        public async Task<decimal> SumAmountByActorTodayAsync( string actorUserId, DateOnly today, CancellationToken cancellationToken = default)
        {
            return await Entities
                .Where(t =>
                    t.ActorUserId == actorUserId &&
                    t.Status == TransactionStatus.Approved &&
                    DateOnly.FromDateTime(t.CreatedAtUtc.UtcDateTime) == today)
                .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;
        }
    }
}
