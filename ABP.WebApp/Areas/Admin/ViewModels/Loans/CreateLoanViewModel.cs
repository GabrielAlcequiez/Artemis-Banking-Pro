namespace ABP.WebApp.Areas.Admin.ViewModels.Loans;

public sealed class CreateLoanViewModel
{
    public string ClientId { get; set; } = string.Empty;

    public string ClientFullName { get; set; } = string.Empty;

    public string ClientIdentification { get; set; } = string.Empty;

    public string ClientEmail { get; set; } = string.Empty;

    public decimal CurrentDebt { get; set; }

    public decimal CapitalAmount { get; set; }

    public int TermInMonths { get; set; }

    public decimal AnnualInterestRate { get; set; }
}
