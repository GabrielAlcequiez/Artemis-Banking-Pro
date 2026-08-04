namespace ABP.Application.Interfaces.Services;

public interface ILoanDelinquencyService
{
    Task<int> UpdateDelinquencyAsync(DateOnly bankingDate, CancellationToken cancellationToken = default);
}
