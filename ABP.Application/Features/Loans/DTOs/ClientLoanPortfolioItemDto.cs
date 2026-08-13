namespace ABP.Application.Features.Loans.DTOs;

public sealed record ClientLoanPortfolioItemDto(
    Guid Id,
    string LoanNumber,
    decimal CapitalAmount,
    decimal PendingAmount,
    decimal MonthlyInstallment,
    decimal AnnualInterestRate,
    int TermInMonths);
