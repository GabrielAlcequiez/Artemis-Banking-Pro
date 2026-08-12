namespace ABP.WebApp.Areas.Admin.ViewModels.SavingsAccounts;

public sealed class CreateSecondaryAccountViewModel
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientFullName { get; set; } = string.Empty;

    public string ClientIdentification { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public decimal InitialBalance { get; set; }
}
