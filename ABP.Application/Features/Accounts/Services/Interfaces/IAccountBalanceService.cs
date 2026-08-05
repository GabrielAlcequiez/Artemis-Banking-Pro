using ABP.Application.Common;

namespace ABP.Application.Features.Accounts.Services.Interfaces;


public interface IAccountBalanceService
{
    Task<OperationResult> CreditAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default);

    Task<OperationResult> DebitAsync( Guid accountId, decimal amount, CancellationToken cancellationToken = default);
}
