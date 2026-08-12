namespace ABP.WebApi.Models.SavingsAccounts;

public sealed class WithdrawApiRequest
{
    public string SourceAccountNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
