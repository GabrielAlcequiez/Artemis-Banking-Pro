using ABP.Application.Common;

namespace ABP.Application.Interfaces.Services;


public interface IPrimaryAccountProvisioner
{
    Task<OperationResult<FinancialOperationReceipt>> OpenPrincipalAccountAsync(string ownerUserId, decimal initialBalance, string actorUserId, string actorRole,
        CancellationToken cancellationToken = default);
}
