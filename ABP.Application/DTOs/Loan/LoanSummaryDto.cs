namespace ABP.Application.DTOs.Loan
{
    public sealed record LoanSummaryDto(
        Guid Id,
        string LoanNumber,
        string ClientId,
        string ClientFullName,
        decimal CapitalAmount,
        int TotalInstallments,
        int PaidInstallments,
        decimal PendingAmount,
        decimal AnnualInterestRate,
        int TermInMonths,
        string Status,
        string ClientPaymentStatus,
        DateTimeOffset CreatedAt);
}
