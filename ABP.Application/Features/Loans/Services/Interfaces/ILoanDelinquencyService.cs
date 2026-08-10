namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanDelinquencyService
{
    Task<int> UpdateDelinquencyAsync(DateOnly bankingDate, CancellationToken cancellationToken = default);
}
