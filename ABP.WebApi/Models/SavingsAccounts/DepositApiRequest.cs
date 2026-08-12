namespace ABP.WebApi.Models.SavingsAccounts;

public sealed class DepositApiRequest
{
    public string DestinationAccountNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
