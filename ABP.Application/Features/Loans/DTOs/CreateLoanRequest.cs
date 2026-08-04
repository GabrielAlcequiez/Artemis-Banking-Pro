namespace ABP.Application.Features.Loans.DTOs
{
    public sealed record CreateLoanRequest(
        string ClientId,
        decimal CapitalAmount,
        int TermInMonths,
        decimal AnnualInterestRate,
        bool ConfirmHighRisk = false);
}
