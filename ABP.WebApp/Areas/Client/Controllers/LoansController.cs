using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Enums;
using ABP.WebApp.Areas.Client.ViewModels.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = nameof(Roles.Client))]
public sealed class LoansController(ILoanService loanService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var loan = await loanService.GetClientDetailAsync(
            id,
            cancellationToken);

        return loan is null
            ? NotFound()
            : View(new LoanDetailViewModel { Loan = loan });
    }
}
