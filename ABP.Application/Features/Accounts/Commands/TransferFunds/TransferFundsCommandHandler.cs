using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.TransferFunds;

public sealed class TransferFundsCommandHandler(IMoneyTransferService moneyTransfer)
    : IRequestHandler<TransferFundsCommand, OperationResult<FinancialOperationReceipt>>
{
    public Task<OperationResult<FinancialOperationReceipt>> Handle(
        TransferFundsCommand command, CancellationToken cancellationToken)
    {
        return moneyTransfer.TransferAsync(command.Request, cancellationToken);
    }
}
