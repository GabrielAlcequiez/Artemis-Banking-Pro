namespace ABP.Application.Common.Services.Interfaces;

public interface ICustomerDebtService
{
    Task<decimal> GetTotalDebtAsync(
        string clientId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, decimal>> GetTotalDebtsAsync(
        IReadOnlyCollection<string> clientIds,
        CancellationToken cancellationToken = default);

    Task<decimal> GetAverageActiveClientDebtAsync(
        CancellationToken cancellationToken = default);
}
