namespace ABP.Application.Interfaces.Services
{
    public interface ICardDebtReaderService
    {
        Task<decimal> GetActiveCardDebtByClientIdAsync(
            string clientId,
            CancellationToken cancellationToken = default);
    }
}