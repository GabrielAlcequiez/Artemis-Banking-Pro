namespace ABP.Application.Features.Accounts.DTOs;

public sealed record CashierWithdrawalPreview(
    Guid AccountId,
    string AccountNumber,
    string AccountOwnerFullName,
    decimal AvailableBalance,
    decimal Amount);
