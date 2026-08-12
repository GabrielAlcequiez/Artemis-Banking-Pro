using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;

namespace ABP.TestDoubles
{
    public class FakeMoneyTransferService : IMoneyTransferService
    {
        public OperationResult<FinancialOperationReceipt>? TransferResult { get; set; }

        public OperationResult<FinancialOperationReceipt>? DepositResult { get; set; }

        public OperationResult<FinancialOperationReceipt>? WithdrawResult { get; set; }

        private static OperationResult<FinancialOperationReceipt> DefaultReceipt(decimal amount) =>
            OperationResult<FinancialOperationReceipt>.Success(new FinancialOperationReceipt(
                Guid.NewGuid(), amount, DateTimeOffset.UtcNow));

        public Task<OperationResult<FinancialOperationReceipt>> TransferAsync(
            TransferFundsRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TransferResult ?? DefaultReceipt(request.Amount));
        }

        public Task<OperationResult<FinancialOperationReceipt>> DepositAsync(
            DepositRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(DepositResult ?? DefaultReceipt(request.Amount));
        }

        public Task<OperationResult<FinancialOperationReceipt>> WithdrawAsync(
            WithdrawalRequest request, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(WithdrawResult ?? DefaultReceipt(request.Amount));
        }
    }
}
