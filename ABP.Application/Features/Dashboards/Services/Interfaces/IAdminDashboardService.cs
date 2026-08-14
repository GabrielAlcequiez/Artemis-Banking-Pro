using ABP.Application.Features.Dashboards.DTOs;

namespace ABP.Application.Features.Dashboards.Services.Interfaces;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(
        CancellationToken cancellationToken = default);
}
