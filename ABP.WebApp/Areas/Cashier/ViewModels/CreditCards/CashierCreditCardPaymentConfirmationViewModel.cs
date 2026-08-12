namespace ABP.WebApp.Areas.Cashier.ViewModels.CreditCards;

public sealed class CashierCreditCardPaymentConfirmationViewModel
{
    public Guid CreditCardId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid OperationId { get; set; }

    public string AccountOwnerFullName { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string CardOwnerFullName { get; set; } = string.Empty;

    public string CardLastFourDigits { get; set; } = string.Empty;

    public decimal RequestedAmount { get; set; }

    public decimal EffectiveAmount { get; set; }
}
