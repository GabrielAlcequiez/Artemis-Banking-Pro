namespace ABP.Application.Features.CreditCards.DTOs;

public sealed record CreditCardOperationOptionDto(
    Guid Id,
    string MaskedCardNumber,
    decimal CurrentDebt,
    decimal AvailableCredit,
    string ExpirationDate);

public sealed record SavingsAccountOperationOptionDto(
    Guid Id,
    string AccountNumber,
    decimal Balance);

public sealed record ClientCardOperationOptions(
    IReadOnlyCollection<CreditCardOperationOptionDto> CreditCards,
    IReadOnlyCollection<SavingsAccountOperationOptionDto> SavingsAccounts);
