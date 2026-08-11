using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.AddBeneficiary;

public sealed record AddBeneficiaryCommand(
    AddBeneficiaryRequest Request) : IRequest<OperationResult<BeneficiaryDto>>;
