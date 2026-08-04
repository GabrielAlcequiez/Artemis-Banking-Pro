using ABP.Domain.Enums;

namespace ABP.Application.DTOs.Account;

public class SavingsAccountSummaryDto
{
    public required Guid Id { get; set; }
    public required string OwnerUserId { get; set; }
    public required string AccountNumber { get; set; }
    public required SavingsAccountType Type { get; set; }
    public required SavingsAccountStatus Status { get; set; }
    public required decimal Balance { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }
}
