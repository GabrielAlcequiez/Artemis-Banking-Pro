namespace ABP.Application.Features.Loans.DTOs
{
    public sealed record LoanDetailDto(
        Guid Id,
        string LoanNumber,
        string ClientId,
        string ClientFullName,
        decimal CapitalAmount,
        decimal AnnualInterestRate,
        int TermInMonths,
        decimal MonthlyInstallment,
        decimal PendingAmount,
        string Status,
        string ClientPaymentStatus,
        DateTimeOffset CreatedAt,
        IReadOnlyCollection<LoanInstallmentDto> Amortization);
}
