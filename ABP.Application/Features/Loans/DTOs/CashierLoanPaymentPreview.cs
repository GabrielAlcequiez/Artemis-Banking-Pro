namespace ABP.Application.Features.Loans.DTOs;

public sealed record CashierLoanPaymentPreview(
    Guid LoanId,
    Guid SourceAccountId,
    Guid OperationId,
    string AccountOwnerFullName,
    string AccountNumber,
    string LoanOwnerFullName,
    string LoanNumber,
    decimal RequestedAmount,
    decimal EffectiveAmount);
