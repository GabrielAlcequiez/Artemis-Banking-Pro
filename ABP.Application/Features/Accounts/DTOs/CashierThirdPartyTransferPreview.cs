namespace ABP.Application.Features.Accounts.DTOs;

public sealed record CashierThirdPartyTransferPreview(
    Guid SourceAccountId,
    string SourceAccountNumber,
    string SourceOwnerFullName,
    decimal SourceAvailableBalance,
    Guid DestinationAccountId,
    string DestinationAccountNumber,
    string DestinationOwnerFullName,
    decimal Amount);
