using ABP.Domain.Enums;

namespace ABP.Domain.ReadModels.Loans;

public sealed record LoanSummaryReadModel(
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
    LoanStatus Status,
    bool HasLateInstallments,
    DateTimeOffset CreatedAt);
