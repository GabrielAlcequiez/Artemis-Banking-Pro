namespace ABP.WebApp.Areas.Cashier.ViewModels.CreditCards;

public sealed class CashierCreditCardPaymentViewModel
{
    public string SourceAccountNumber { get; set; } = string.Empty;

    public string CreditCardNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public Guid OperationId { get; set; }
}
