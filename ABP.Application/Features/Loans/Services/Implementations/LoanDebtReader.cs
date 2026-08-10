using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoanDebtReader(ILoanRepository repository) : ILoanDebtReader
{
    public Task<decimal> GetActiveLoanDebtByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return Task.FromResult(0m);
        }

        return repository.GetActiveDebtByClientIdAsync(clientId, cancellationToken);
    }
}
