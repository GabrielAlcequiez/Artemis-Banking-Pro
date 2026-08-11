using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.Withdraw;

public sealed class WithdrawCommandHandler(IMoneyTransferService moneyTransfer)
    : IRequestHandler<WithdrawCommand, OperationResult<FinancialOperationReceipt>>
{
    public Task<OperationResult<FinancialOperationReceipt>> Handle(
        WithdrawCommand command, CancellationToken cancellationToken)
    {
        return moneyTransfer.WithdrawAsync(command.Request, cancellationToken);
    }
}
