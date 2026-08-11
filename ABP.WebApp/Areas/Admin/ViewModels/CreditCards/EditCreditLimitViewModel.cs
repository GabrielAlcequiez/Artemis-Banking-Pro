namespace ABP.WebApp.Areas.Admin.ViewModels.CreditCards;

public sealed class EditCreditLimitViewModel
{
    public Guid CreditCardId { get; set; }

    public string MaskedCardNumber { get; set; } = string.Empty;

    public decimal CurrentDebt { get; set; }

    public decimal CreditLimit { get; set; }
}
