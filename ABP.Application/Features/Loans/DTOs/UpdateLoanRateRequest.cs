namespace ABP.Application.Features.Loans.DTOs
{
    public sealed record UpdateLoanRateRequest(
        Guid LoanId,
        decimal AnnualInterestRate);
}
