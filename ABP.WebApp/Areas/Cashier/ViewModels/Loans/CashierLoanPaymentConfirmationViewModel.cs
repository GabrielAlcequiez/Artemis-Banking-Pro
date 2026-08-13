namespace ABP.WebApp.Areas.Cashier.ViewModels.Loans;

public sealed class CashierLoanPaymentConfirmationViewModel
{
    public Guid LoanId { get; set; }

    public Guid SourceAccountId { get; set; }

    public Guid OperationId { get; set; }

    public string AccountOwnerFullName { get; set; } = string.Empty;

    public string AccountNumber { get; set; } = string.Empty;

    public string LoanOwnerFullName { get; set; } = string.Empty;

    public string LoanNumber { get; set; } = string.Empty;

    public decimal RequestedAmount { get; set; }

    public decimal EffectiveAmount { get; set; }
}
