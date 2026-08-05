using ABP.Domain.Entities.CreditCards;

namespace ABP.Domain.Interfaces;

public interface ICreditCardRepository : IGenericRepository<CreditCard, Guid>
{
    Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default);
    Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default);
    Task AddConsumptionAsync(CardConsumption consumption, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(CardPayment payment, CancellationToken cancellationToken = default);
}