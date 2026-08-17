namespace ABP.WebApp.Areas.Cashier.ViewModels.Accounts;

public sealed class CashierWithdrawalConfirmationViewModel
{
    public string AccountNumber { get; set; } = string.Empty;

    public string AccountOwnerFullName { get; set; } = string.Empty;

    public decimal AvailableBalance { get; set; }

    public decimal Amount { get; set; }
}
