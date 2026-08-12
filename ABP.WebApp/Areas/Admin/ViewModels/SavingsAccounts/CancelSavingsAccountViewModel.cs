namespace ABP.WebApp.Areas.Admin.ViewModels.SavingsAccounts;

public sealed class CancelSavingsAccountViewModel
{
    public Guid AccountId { get; set; }

    public string AccountNumber { get; set; } = string.Empty;
}
