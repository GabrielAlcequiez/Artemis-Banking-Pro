using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakeSavingsAccountAdminService : ISavingsAccountAdminService
    {
        public OperationResult<Guid>? CreateSecondaryAccountResult { get; set; }

        public OperationResult? CancelResult { get; set; }

        public Task<OperationResult<Guid>> CreateSecondaryAccountAsync(
            CreateSecondaryAccountRequest request, CancellationToken cancellationToken = default)
        {
            var result = CreateSecondaryAccountResult ?? OperationResult<Guid>.Success(Guid.NewGuid());
            return Task.FromResult(result);
        }

        public Task<OperationResult> CancelAsync(
            CancelSavingsAccountRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(CancelResult ?? OperationResult.Success());
        }
    }
}
