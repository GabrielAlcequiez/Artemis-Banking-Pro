namespace ABP.Domain.ReadModels.Loans;

public sealed record ClientLoanPortfolioReadModel(
    Guid Id,
    string LoanNumber,
    decimal CapitalAmount,
    decimal PendingAmount,
    decimal MonthlyInstallment,
    decimal AnnualInterestRate,
    int TermInMonths);
