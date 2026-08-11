using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;
using MediatR;

namespace ABP.Application.Features.CreditCards.Commands.UpdateCreditLimit;

public sealed record UpdateCreditLimitCommand(
    UpdateCreditLimitRequest Request) : IRequest<OperationResult>;
