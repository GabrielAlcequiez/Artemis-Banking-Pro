namespace ABP.WebApp.Areas.Cashier.ViewModels.Loans;

public sealed class CashierLoanPaymentViewModel
{
    public string SourceAccountNumber { get; set; } = string.Empty;

    public string LoanNumber { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public Guid OperationId { get; set; }
}
