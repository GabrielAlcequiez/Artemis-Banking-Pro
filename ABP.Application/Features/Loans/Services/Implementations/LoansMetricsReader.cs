using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Interfaces;

namespace ABP.Application.Features.Loans.Services.Implementations;

public sealed class LoansMetricsReader(ILoanRepository repository)
    : ILoansMetricsReader
{
    public Task<int> CountActiveLoansAsync(
        CancellationToken cancellationToken = default) =>
        repository.CountActiveLoansAsync(cancellationToken);
}
