using ABP.Application.Features.Loans.DTOs;

namespace ABP.WebApp.Areas.Client.ViewModels.Loans;

public sealed class LoanDetailViewModel
{
    public required LoanDetailDto Loan { get; init; }
}
