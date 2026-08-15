using ABP.Application.Features.Dashboards.Services.Interfaces;
using ABP.WebApp.Areas.Admin.ViewModels.Home;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Administrator")]
    public class HomeController(IAdminDashboardService dashboardService) : Controller
    {
        public async Task<IActionResult> Index(
            CancellationToken cancellationToken)
        {
            var dashboard = await dashboardService.GetDashboardAsync(
                cancellationToken);

            return View(new AdminHomeViewModel { Dashboard = dashboard });
        }
    }
}
