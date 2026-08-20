using ABP.Application.Features.Dashboards.DTOs;

namespace ABP.Application.Features.Dashboards.Services.Interfaces;

public interface IClientPortfolioService
{
    Task<ClientPortfolioDto> GetPortfolioAsync(
        CancellationToken cancellationToken = default);
}
