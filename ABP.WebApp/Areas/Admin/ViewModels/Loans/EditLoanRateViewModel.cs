namespace ABP.WebApp.Areas.Admin.ViewModels.Loans;

public sealed class EditLoanRateViewModel
{
    public Guid LoanId { get; set; }

    public string LoanNumber { get; set; } = string.Empty;

    public string ClientFullName { get; set; } = string.Empty;

    public decimal PendingAmount { get; set; }

    public string Status { get; set; } = string.Empty;

    public decimal CurrentAnnualInterestRate { get; set; }

    public decimal AnnualInterestRate { get; set; }
}
