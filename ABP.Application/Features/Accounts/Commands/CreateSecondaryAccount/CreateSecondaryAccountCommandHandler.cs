using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.CreateSecondaryAccount;

public sealed class CreateSecondaryAccountCommandHandler(ISavingsAccountAdminService adminService)
    : IRequestHandler<CreateSecondaryAccountCommand, OperationResult<Guid>>
{
    public Task<OperationResult<Guid>> Handle(
        CreateSecondaryAccountCommand command, CancellationToken cancellationToken)
    {
        return adminService.CreateSecondaryAccountAsync(command.Request, cancellationToken);
    }
}
