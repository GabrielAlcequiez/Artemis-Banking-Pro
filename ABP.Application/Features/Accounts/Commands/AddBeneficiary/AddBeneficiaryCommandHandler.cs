using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using ABP.Application.Features.Accounts.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.AddBeneficiary;

public sealed class AddBeneficiaryCommandHandler(IBeneficiaryService beneficiaries)
    : IRequestHandler<AddBeneficiaryCommand, OperationResult<BeneficiaryDto>>
{
    public Task<OperationResult<BeneficiaryDto>> Handle(
        AddBeneficiaryCommand command, CancellationToken cancellationToken)
    {
        return beneficiaries.AddAsync(command.Request, cancellationToken);
    }
}
