namespace ABP.WebApp.Areas.Cashier.ViewModels.Accounts;

public sealed class CashierThirdPartyTransferConfirmationViewModel
{
    public Guid SourceAccountId { get; set; }

    public string SourceAccountNumber { get; set; } = string.Empty;

    public string SourceOwnerFullName { get; set; } = string.Empty;

    public Guid DestinationAccountId { get; set; }

    public string DestinationAccountNumber { get; set; } = string.Empty;

    public string DestinationOwnerFullName { get; set; } = string.Empty;

    public decimal Amount { get; set; }
}
