using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;

namespace ABP.Application.Features.Accounts.Services.Interfaces;

public interface ICashierAccountOperationService
{
    Task<OperationResult<CashierDepositPreview>> PrepareDepositAsync(
        string accountNumber, decimal amount, CancellationToken cancellationToken = default);

    Task<OperationResult<CashierWithdrawalPreview>> PrepareWithdrawalAsync(
        string accountNumber, decimal amount, CancellationToken cancellationToken = default);

    Task<OperationResult<CashierThirdPartyTransferPreview>> PrepareThirdPartyTransferAsync(
        string sourceAccountNumber, string destinationAccountNumber, decimal amount,
        CancellationToken cancellationToken = default);
}
