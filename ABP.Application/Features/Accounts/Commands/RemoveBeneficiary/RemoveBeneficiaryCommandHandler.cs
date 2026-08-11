using ABP.Application.Common;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.RemoveBeneficiary;

public sealed class RemoveBeneficiaryCommandHandler(IBeneficiaryService beneficiaries)
    : IRequestHandler<RemoveBeneficiaryCommand, OperationResult>
{
    public Task<OperationResult> Handle(
        RemoveBeneficiaryCommand command, CancellationToken cancellationToken)
    {
        return beneficiaries.RemoveAsync(command.OwnerUserId, command.BeneficiaryId, cancellationToken);
    }
}
