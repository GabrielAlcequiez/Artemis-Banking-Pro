namespace ABP.Application.Features.Accounts.DTOs;

public class CreateSecondaryAccountRequest
{
    public required string OwnerUserId { get; set; }
    public required decimal InitialBalance { get; set; }
    public required string ActorUserId { get; set; }
    public required string ActorRole { get; set; }
}
