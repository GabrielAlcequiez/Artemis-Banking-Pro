namespace ABP.Application.Features.CreditCards.Services.Interfaces;

public interface ICustomerDebtSnapshotReader
{
    Task<decimal> GetTotalDebtAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    Task<decimal> GetAverageActiveClientDebtAsync(
        CancellationToken cancellationToken = default);
}
