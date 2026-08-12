using ABP.Application.Features.Accounts.DTOs;

namespace ABP.WebApi.Models.SavingsAccounts;

public sealed record SavingsAccountCreatedResponse(
    Guid Id,
    string OwnerUserId,
    string AccountNumber,
    string Type,
    string Status,
    decimal Balance,
    DateTimeOffset CreatedAtUtc)
{
    public static SavingsAccountCreatedResponse From(SavingsAccountDetailDto detail) =>
        new(
            detail.Id,
            detail.OwnerUserId,
            detail.AccountNumber,
            detail.Type.ToString(),
            detail.Status.ToString(),
            detail.Balance,
            detail.CreatedAtUtc);
}
