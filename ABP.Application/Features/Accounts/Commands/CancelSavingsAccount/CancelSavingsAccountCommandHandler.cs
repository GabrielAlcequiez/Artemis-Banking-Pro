using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.CancelSavingsAccount;

public sealed class CancelSavingsAccountCommandHandler(ISavingsAccountAdminService adminService)
    : IRequestHandler<CancelSavingsAccountCommand, OperationResult>
{
    public Task<OperationResult> Handle(
        CancelSavingsAccountCommand command, CancellationToken cancellationToken)
    {
        return adminService.CancelAsync(command.Request, cancellationToken);
    }
}
