using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;
using ABP.Domain.Enums;

namespace ABP.Application.Features.Accounts.Services.Interfaces;

public interface ISavingsAccountQueryService
{
    Task<PagedResult<SavingsAccountSummaryDto>> ListAsync(
        PagedRequest pagedRequest,
        string? ownerIdentification,
        SavingsAccountStatus? status,
        SavingsAccountType? type,
        CancellationToken cancellationToken = default);

    Task<SavingsAccountDetailDto?> GetDetailAsync(
        Guid accountId, CancellationToken cancellationToken = default);
}
