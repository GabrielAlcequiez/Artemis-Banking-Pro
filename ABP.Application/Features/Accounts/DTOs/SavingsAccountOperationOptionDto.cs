namespace ABP.Application.Features.Accounts.DTOs;

public sealed record SavingsAccountOperationOptionDto(
    Guid Id,
    string AccountNumber,
    decimal Balance);
