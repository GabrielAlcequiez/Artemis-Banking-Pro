using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories;

public sealed class HermesTransactionRepository(AppDbContext context)
    : IHermesTransactionRepository
{
    public async Task<PagedResult<HermesTransactionReadModel>> GetByCommerceAsync(
        Guid commerceId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.CardConsumptions
            .AsNoTracking()
            .Where(consumption => consumption.CommerceId == commerceId)
            .Join(
                context.CreditCards.AsNoTracking(),
                consumption => consumption.CreditCardId,
                card => card.Id,
                (consumption, card) => new { Consumption = consumption, Card = card });

        var totalRecords = await query.CountAsync(cancellationToken);
        var skip = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);
        var data = await query
            .OrderByDescending(item => item.Consumption.OccurredAtUtc)
            .ThenByDescending(item => item.Consumption.Id)
            .Skip(skip)
            .Take(pageSize)
            .Select(item => new HermesTransactionReadModel(
                item.Consumption.Id,
                item.Consumption.OccurredAtUtc,
                item.Consumption.Amount,
                item.Card.CardNumber.Substring(12, 4),
                item.Consumption.Status))
            .ToListAsync(cancellationToken);

        return new PagedResult<HermesTransactionReadModel>(
            data,
            page,
            pageSize,
            totalRecords);
    }
}
