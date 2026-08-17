using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.Persistence.Repositories;

public class CreditCardRepository(AppDbContext context) : GenericRepository<CreditCard, Guid>(context), ICreditCardRepository
{
    private const string MaskPrefix = "************";

    #region Card persistence

    public Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(card => card.CardNumber == cardNumber, cancellationToken);
    }

    public Task<CreditCard?> GetByCardNumberForUpdateAsync(
        string cardNumber,
        CancellationToken cancellationToken = default)
    {
        return Entities.SingleOrDefaultAsync(
            card => card.CardNumber == cardNumber,
            cancellationToken);
    }

    public Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .AnyAsync(card => card.CardNumber == cardNumber, cancellationToken);
    }

    public Task<CreditCard?> GetByCreationOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        Entities
            .AsNoTracking()
            .SingleOrDefaultAsync(
                card => card.CreationOperationId == operationId,
                cancellationToken);

    public async Task AddConsumptionAsync(CardConsumption consumption, CancellationToken cancellationToken = default)
    {
        await _context.CardConsumptions.AddAsync(consumption, cancellationToken);
    }

    public async Task AddPaymentAsync(CardPayment payment, CancellationToken cancellationToken = default)
    {
        await _context.CardPayments.AddAsync(payment, cancellationToken);
    }

    public Task<CardPayment?> GetPaymentByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        _context.CardPayments
            .AsNoTracking()
            .SingleOrDefaultAsync(
                payment => payment.OperationId == operationId,
                cancellationToken);

    public Task<CardConsumption?> GetConsumptionByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        _context.CardConsumptions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                consumption => consumption.OperationId == operationId,
                cancellationToken);

    public async Task<IReadOnlyCollection<CreditCard>> GetActiveByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        await Entities
            .AsNoTracking()
            .Where(card =>
                card.ClientId == clientId &&
                card.Status == CreditCardStatus.Active)
            .OrderByDescending(card => card.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    #endregion

    #region Administrative card queries

    public Task<string?> FindClientIdByIdentificationAsync(
        string identification,
        CancellationToken cancellationToken = default)
    {
        var normalizedIdentification = identification.Trim();

        return _context.Users
            .AsNoTracking()
            .Where(user => user.Identification == normalizedIdentification && user.Role == Roles.Client)
            .Select(user => user.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasAnyCardsAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .AsNoTracking()
            .AnyAsync(card => card.ClientId == clientId, cancellationToken);
    }

    public async Task<PagedResult<CreditCardSummaryReadModel>> SearchAsync(
        int page,
        int pageSize,
        string? identification = null,
        CreditCardStatusFilter? status = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedPageSize = Math.Clamp(pageSize, 1, 20);

        var clientsQuery = _context.Users
            .AsNoTracking()
            .Where(client => client.Role == Roles.Client);

        var query = _context.CreditCards
            .AsNoTracking()
            .Join(
                clientsQuery,
                card => card.ClientId,
                client => client.Id,
                (card, client) => new { Card = card, Client = client });

        var normalizedIdentification = identification?.Trim();
        var hasIdentification = !string.IsNullOrWhiteSpace(normalizedIdentification);

        if (hasIdentification)
        {
            query = query.Where(item => item.Client.Identification == normalizedIdentification!);
        }

        switch (status)
        {
            case null:
                if (!hasIdentification)
                {
                    query = query.Where(item => item.Card.Status == CreditCardStatus.Active);
                }
                break;
            case CreditCardStatusFilter.Active:
                query = query.Where(item => item.Card.Status == CreditCardStatus.Active);
                break;
            case CreditCardStatusFilter.Cancelled:
                query = query.Where(item => item.Card.Status == CreditCardStatus.Cancelled);
                break;
            case CreditCardStatusFilter.All:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown credit card status filter.");
        }

        var totalRecords = await query.CountAsync(cancellationToken);

        var includeAllStatuses = status == CreditCardStatusFilter.All
            || (status is null && hasIdentification);
        var orderedQuery = includeAllStatuses
            ? query
                .OrderBy(item => item.Card.Status == CreditCardStatus.Active ? 0 : 1)
                .ThenByDescending(item => item.Card.CreatedAtUtc)
            : query.OrderByDescending(item => item.Card.CreatedAtUtc);

        var skip = (int)Math.Min((long)(normalizedPage - 1) * normalizedPageSize, int.MaxValue);
        var data = await orderedQuery
            .Skip(skip)
            .Take(normalizedPageSize)
            .Select(item => new CreditCardSummaryReadModel(
                item.Card.Id,
                MaskPrefix + item.Card.CardNumber.Substring(12, 4),
                item.Card.CardNumber.Substring(12, 4),
                item.Card.ClientId,
                item.Client.Name + " " + item.Client.LastName,
                item.Card.Limit,
                item.Card.Limit - item.Card.Debt,
                item.Card.Debt,
                item.Card.ExpirationDate,
                item.Card.Status,
                item.Card.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CreditCardSummaryReadModel>(data, normalizedPage, normalizedPageSize, totalRecords);
    }

    public Task<CreditCardDetailReadModel?> GetDetailsAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default) =>
        GetDetailsCoreAsync(creditCardId, clientId: null, cancellationToken);

    public Task<CreditCardDetailReadModel?> GetDetailsForClientAsync(
        Guid creditCardId,
        string clientId,
        CancellationToken cancellationToken = default) =>
        GetDetailsCoreAsync(creditCardId, clientId, cancellationToken);

    private async Task<CreditCardDetailReadModel?> GetDetailsCoreAsync(
        Guid creditCardId,
        string? clientId,
        CancellationToken cancellationToken)
    {
        var clientsQuery = _context.Users
            .AsNoTracking()
            .Where(client => client.Role == Roles.Client);

        var cardsQuery = _context.CreditCards
            .AsNoTracking()
            .Where(card => card.Id == creditCardId);

        if (clientId is not null)
        {
            cardsQuery = cardsQuery.Where(card => card.ClientId == clientId);
        }

        var summary = await cardsQuery
            .Join(
                clientsQuery,
                card => card.ClientId,
                client => client.Id,
                (card, client) => new { Card = card, Client = client })
            .Select(item => new CreditCardSummaryReadModel(
                item.Card.Id,
                MaskPrefix + item.Card.CardNumber.Substring(12, 4),
                item.Card.CardNumber.Substring(12, 4),
                item.Card.ClientId,
                item.Client.Name + " " + item.Client.LastName,
                item.Card.Limit,
                item.Card.Limit - item.Card.Debt,
                item.Card.Debt,
                item.Card.ExpirationDate,
                item.Card.Status,
                item.Card.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);

        if (summary is null)
        {
            return null;
        }

        var consumptions = await _context.CardConsumptions
            .AsNoTracking()
            .Where(consumption => consumption.CreditCardId == creditCardId)
            .OrderByDescending(consumption => consumption.OccurredAtUtc)
            .Select(consumption => new CardConsumptionReadModel(
                consumption.Id,
                consumption.OccurredAtUtc,
                consumption.Amount,
                consumption.CommerceId == null ? "AVANCE" : consumption.CommerceName,
                consumption.Status))
            .ToListAsync(cancellationToken);

        return new CreditCardDetailReadModel(
            summary.Id,
            summary.MaskedCardNumber,
            summary.LastFourDigits,
            summary.ClientId,
            summary.ClientFullName,
            summary.CreditLimit,
            summary.AvailableCredit,
            summary.CurrentDebt,
            summary.ExpirationDate,
            summary.Status,
            summary.CreatedAt,
            consumptions);
    }

    #endregion

    #region Debt and lifecycle queries

    public async Task<decimal> GetActiveDebtByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var debt = await _context.CreditCards
            .AsNoTracking()
            .Where(card => card.ClientId == clientId && card.Status == CreditCardStatus.Active)
            .Select(card => (decimal?)card.Debt)
            .SumAsync(cancellationToken);

        return debt ?? 0m;
    }

    public async Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(
        IReadOnlyCollection<string> clientIds,
        CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        return await _context.CreditCards
            .AsNoTracking()
            .Where(card =>
                clientIds.Contains(card.ClientId)
                && card.Status == CreditCardStatus.Active)
            .GroupBy(card => card.ClientId)
            .Select(group => new
            {
                ClientId = group.Key,
                Debt = group.Sum(card => card.Debt)
            })
            .ToDictionaryAsync(
                item => item.ClientId,
                item => item.Debt,
                cancellationToken);
    }

    public async Task<decimal> GetTotalActiveDebtForActiveClientsAsync(
        CancellationToken cancellationToken = default)
    {
        var activeClients = _context.Users
            .AsNoTracking()
            .Where(client => client.Role == Roles.Client && client.IsActive);

        var debt = await _context.CreditCards
            .AsNoTracking()
            .Where(card => card.Status == CreditCardStatus.Active)
            .Join(
                activeClients,
                card => card.ClientId,
                client => client.Id,
                (card, _) => (decimal?)card.Debt)
            .SumAsync(cancellationToken);

        return debt ?? 0m;
    }

    public Task<bool> IsActiveClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                user =>
                    user.Id == clientId &&
                    user.Role == Roles.Client &&
                    user.IsActive,
                cancellationToken);
    }

    public Task<bool> ClientExistsAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        return _context.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.Id == clientId && user.Role == Roles.Client,
                cancellationToken);
    }

    public Task<CreditCard?> GetForUpdateAsync(Guid creditCardId, CancellationToken cancellationToken = default)
    {
        return Entities.SingleOrDefaultAsync(
            card => card.Id == creditCardId,
            cancellationToken);
    }

    #endregion
}
