namespace ABP.Application.Features.CreditCards.Services.Interfaces
{
    public interface ICardDebtReaderService
    {
        Task<decimal> GetActiveCardDebtByClientIdAsync(
            string clientId,
            CancellationToken cancellationToken = default);
    }
}