namespace ABP.WebApp.Areas.Admin.ViewModels.CreditCards;

public sealed class CancelCreditCardViewModel
{
    public Guid CreditCardId { get; set; }

    public string LastFourDigits { get; set; } = string.Empty;
}
