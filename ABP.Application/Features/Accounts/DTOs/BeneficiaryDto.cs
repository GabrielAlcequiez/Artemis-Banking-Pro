namespace ABP.Application.Features.Accounts.DTOs;

public class BeneficiaryDto
{
    public required Guid Id { get; set; }
    public required Guid BeneficiaryAccountId { get; set; }
    public required string BeneficiaryAccountNumber { get; set; }
    public required string BeneficiaryOwnerName { get; set; }
    public required DateTimeOffset CreatedAtUtc { get; set; }
}
