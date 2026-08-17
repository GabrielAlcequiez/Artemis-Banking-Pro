namespace ABP.WebApp.Areas.Cashier.ViewModels.Accounts;

public sealed class CashierDepositConfirmationViewModel
{
    public string AccountNumber { get; set; } = string.Empty;

    public string AccountOwnerFullName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
