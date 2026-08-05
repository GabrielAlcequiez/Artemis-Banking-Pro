using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Common;

namespace ABP.Application.Interfaces.Services;


public interface IMoneyTransferService
{
    Task<OperationResult<FinancialOperationReceipt>> TransferAsync( TransferFundsRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<FinancialOperationReceipt>> DepositAsync( DepositRequest request, CancellationToken cancellationToken = default);

    Task<OperationResult<FinancialOperationReceipt>> WithdrawAsync( WithdrawalRequest request, CancellationToken cancellationToken = default);
}
