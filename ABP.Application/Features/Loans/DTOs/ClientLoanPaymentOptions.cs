namespace ABP.Application.Features.Loans.DTOs;

public sealed record LoanOperationOptionDto(
    Guid Id,
    string LoanNumber,
    decimal PendingAmount);

public sealed record SavingsAccountOperationOptionDto(
    Guid Id,
    string AccountNumber,
    decimal Balance);

public sealed record ClientLoanPaymentOptions(
    IReadOnlyCollection<LoanOperationOptionDto> Loans,
    IReadOnlyCollection<SavingsAccountOperationOptionDto> SavingsAccounts);
