using ABP.Application.Features.Dashboards.DTOs;

namespace ABP.WebApp.Areas.Admin.ViewModels.Home;

public sealed class AdminHomeViewModel
{
    public AdminDashboardDto Dashboard { get; init; } =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0m);
}
