namespace ABP.Application.Interfaces.Services;

public interface ICardsMetricsReaderService
{
    Task<int> GetActiveCardsCountAsync(
        CancellationToken cancellationToken = default);

    Task<decimal> GetTotalActiveCardDebtAsync(
        CancellationToken cancellationToken = default);
}