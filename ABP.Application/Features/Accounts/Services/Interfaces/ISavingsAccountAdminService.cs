using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;

namespace ABP.Application.Features.Accounts.Services.Interfaces
{
    
    public interface ISavingsAccountAdminService
    {
        Task<OperationResult<Guid>> CreateSecondaryAccountAsync(
            CreateSecondaryAccountRequest request, CancellationToken cancellationToken = default);

        Task<OperationResult> CancelAsync(
            CancelSavingsAccountRequest request, CancellationToken cancellationToken = default);
    }
}
