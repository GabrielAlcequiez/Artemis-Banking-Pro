using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakePrimaryAccountProvisioner : IPrimaryAccountProvisioner
    {
        public OperationResult<FinancialOperationReceipt>? Result { get; set; }

        public Task<OperationResult<FinancialOperationReceipt>> OpenPrincipalAccountAsync(
            string ownerUserId, decimal initialBalance, string actorUserId, string actorRole,
            CancellationToken cancellationToken = default)
        {
            var result = Result ?? OperationResult<FinancialOperationReceipt>.Success(new FinancialOperationReceipt(
                Guid.NewGuid(), initialBalance, DateTimeOffset.UtcNow));

            return Task.FromResult(result);
        }
    }
}
