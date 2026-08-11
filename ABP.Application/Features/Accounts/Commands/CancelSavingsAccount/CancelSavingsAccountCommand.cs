using ABP.Application.Common;
using ABP.Application.Features.Accounts.DTOs;
using MediatR;

namespace ABP.Application.Features.Accounts.Commands.CancelSavingsAccount;

public sealed record CancelSavingsAccountCommand(
    CancelSavingsAccountRequest Request) : IRequest<OperationResult>;
