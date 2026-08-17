namespace ABP.WebApp.Areas.Cashier.ViewModels.Accounts;

public sealed class CashierThirdPartyTransferViewModel
{
    public string SourceAccountNumber { get; set; } = string.Empty;

    public string DestinationAccountNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
