using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.WebApp.Areas.Cashier.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [Authorize(Roles = "Cashier")]
    public class HomeController(
        ITransactionsMetricsReader metricsReader,
        ICurrentUserService currentUser) : Controller
    {
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var summary = await metricsReader.GetCashierDailySummaryAsync(
                currentUser.UserId ?? string.Empty, cancellationToken);

            return View(new CashierHomeViewModel { Summary = summary });
        }
    }
}
