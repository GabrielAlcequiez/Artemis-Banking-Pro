using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;

namespace ABP.Application.Features.Accounts.Services.Interfaces;

public interface IAccountClientSelectionService
{
    Task<PagedResult<AccountClientCandidateDto>> SearchAsync(
        AccountClientSearchRequest request, CancellationToken cancellationToken = default);

    Task<AccountClientCandidateDto?> GetActiveClientAsync(
        string clientId, CancellationToken cancellationToken = default);
}
