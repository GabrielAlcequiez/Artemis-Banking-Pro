namespace ABP.WebApp.Areas.Cashier.ViewModels.Accounts;

public sealed class CashierWithdrawalViewModel
{
    public string AccountNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
