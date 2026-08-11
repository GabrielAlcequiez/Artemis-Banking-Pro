using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.CreateSecondaryAccount;

public sealed record CreateSecondaryAccountCommand(
    CreateSecondaryAccountRequest Request) : IRequest<OperationResult<Guid>>;
