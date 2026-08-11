namespace ABP.WebApp.Areas.Admin.ViewModels.CreditCards;

public sealed class CreateCreditCardViewModel
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientFullName { get; set; } = string.Empty;

    public string ClientIdentification { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public decimal CreditLimit { get; set; }
}
