using ABP.Domain.Entities.Cards;
using ABP.Domain.Interfaces;

namespace ABP.Application.Interfaces.Persistence
{
    public interface ICreditCardRepository : IGenericRepository<CreditCard, Guid>
    {
        Task<CreditCard?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default);
        Task<bool> CardNumberExistsAsync(string cardNumber, CancellationToken cancellationToken = default);
        Task AddConsumptionAsync(CardConsumption consumption, CancellationToken cancellationToken = default);
        Task AddPaymentAsync(CardPayment payment, CancellationToken cancellationToken = default);
    }
}
