namespace ABP.Application.Features.Accounts.DTOs;

public class AddBeneficiaryRequest
{
    public required string OwnerUserId { get; set; }
    public required string BeneficiaryAccountNumber { get; set; }
}
