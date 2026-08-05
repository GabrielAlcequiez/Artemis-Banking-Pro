namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanDebtReader
{
    Task<decimal> GetActiveLoanDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
}
