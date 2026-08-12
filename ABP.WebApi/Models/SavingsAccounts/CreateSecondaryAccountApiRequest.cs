namespace ABP.WebApi.Models.SavingsAccounts;

public sealed class CreateSecondaryAccountApiRequest
{
    public string OwnerUserId { get; set; } = string.Empty;

    public decimal InitialBalance { get; set; }
}
