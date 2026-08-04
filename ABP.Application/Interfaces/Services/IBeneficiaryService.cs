using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Common;
using ABP.Application.DTOs.Account;

namespace ABP.Application.Interfaces.Services;

public interface IBeneficiaryService
{
    Task<IReadOnlyCollection<BeneficiaryDto>> ListAsync(string ownerUserId, CancellationToken cancellationToken = default);

    Task<OperationResult<BeneficiaryDto>> AddAsync(AddBeneficiaryRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult> RemoveAsync(string ownerUserId, Guid beneficiaryId, CancellationToken cancellationToken = default);
}
