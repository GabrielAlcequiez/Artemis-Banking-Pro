namespace ABP.Application.Features.Accounts.DTOs;

public class CancelSavingsAccountRequest
{
    public required Guid AccountId { get; set; }
    public required string ActorUserId { get; set; }
    public required string ActorRole { get; set; }
}
