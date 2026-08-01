namespace ABP.Application.Features.Accounts.DTOs;

public class WithdrawalRequest
{
    public required string SourceAccountNumber { get; set; }
    public required decimal Amount { get; set; }
    public required string ActorUserId { get; set; }
    public required string ActorRole { get; set; }
}
