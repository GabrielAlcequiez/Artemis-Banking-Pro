using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.Deposit;

public sealed class DepositCommandHandler(IMoneyTransferService moneyTransfer)
    : IRequestHandler<DepositCommand, OperationResult<FinancialOperationReceipt>>
{
    public Task<OperationResult<FinancialOperationReceipt>> Handle(
        DepositCommand command, CancellationToken cancellationToken)
    {
        return moneyTransfer.DepositAsync(command.Request, cancellationToken);
    }
}
