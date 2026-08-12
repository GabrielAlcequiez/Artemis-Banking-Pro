namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoansMetricsReader
{
    Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default);
}
