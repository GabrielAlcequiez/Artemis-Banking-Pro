using ABP.Application.Common;

namespace ABP.Application.Features.Accounts.Services.Interfaces;


public interface IPrimaryAccountProvisioner
{
    Task<OperationResult<FinancialOperationReceipt>> OpenPrincipalAccountAsync(string ownerUserId, decimal initialBalance, string actorUserId, string actorRole,
        CancellationToken cancellationToken = default);
}
