using ABP.Application.Features.Dashboards.Services.Interfaces;
using ABP.WebApp.Areas.Client.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(Roles = "Client")]
    public class HomeController(
        IClientPortfolioService portfolioService) : Controller
    {
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var portfolio = await portfolioService.GetPortfolioAsync(
                cancellationToken);

            return View(new ClientHomeViewModel
            {
                Accounts = portfolio.Accounts,
                ActiveLoan = portfolio.ActiveLoan,
                CreditCards = portfolio.CreditCards
            });
        }
    }
}
