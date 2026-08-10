using ABP.Application.Features.CreditCards.DTOs;
using ABP.Domain.Common;

namespace ABP.Application.Features.CreditCards.Services.Interfaces;

public interface IActiveClientReader
{
    Task<PagedResult<ActiveClientSummaryDto>> SearchAsync(
        CreditCardClientSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<ActiveClientSummaryDto?> GetByIdAsync(
        string clientId,
        CancellationToken cancellationToken = default);
}
