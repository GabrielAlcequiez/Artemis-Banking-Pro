namespace ABP.Application.Features.Accounts.DTOs;

public sealed record CashierDepositPreview(
    Guid AccountId,
    string AccountNumber,
    string AccountOwnerFullName,
    decimal Amount);
