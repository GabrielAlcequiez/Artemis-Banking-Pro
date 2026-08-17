using ABP.Domain.Common;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.CreditCards;

namespace ABP.Domain.Interfaces;

public interface ICreditCardRepository : IGenericRepository<CreditCard, Guid>
{
    Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default);
    Task<CreditCard?> GetByCardNumberForUpdateAsync(
        string cardNumber,
        CancellationToken cancellationToken = default) =>
        GetByCardNumberAsync(cardNumber, cancellationToken);
    Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default);
    Task<CreditCard?> GetByCreationOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);
    Task AddConsumptionAsync(CardConsumption consumption, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(CardPayment payment, CancellationToken cancellationToken = default);
    Task<CardPayment?> GetPaymentByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);
    Task<CardConsumption?> GetConsumptionByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CreditCard>> GetActiveByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    Task<string?> FindClientIdByIdentificationAsync(string identification, CancellationToken cancellationToken = default);
    Task<bool> HasAnyCardsAsync(string clientId, CancellationToken cancellationToken = default);
    Task<PagedResult<CreditCardSummaryReadModel>> SearchAsync(
        int page,
        int pageSize,
        string? identification = null,
        CreditCardStatusFilter? status = null,
        CancellationToken cancellationToken = default);
    Task<CreditCardDetailReadModel?> GetDetailsAsync(Guid creditCardId, CancellationToken cancellationToken = default);
    Task<CreditCardDetailReadModel?> GetDetailsForClientAsync(
        Guid creditCardId,
        string clientId,
        CancellationToken cancellationToken = default);
    Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default);

    Task<bool> IsActiveClientAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    Task<bool> ClientExistsAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    Task<CreditCard?> GetForUpdateAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default);
}
