namespace ABP.Application.Interfaces.Services;

public interface ILoanDebtReader
{
    Task<decimal> GetActiveLoanDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
