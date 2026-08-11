using ABP.Application.Common;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.RemoveBeneficiary;

public sealed record RemoveBeneficiaryCommand(
    string OwnerUserId, Guid BeneficiaryId) : IRequest<OperationResult>;
