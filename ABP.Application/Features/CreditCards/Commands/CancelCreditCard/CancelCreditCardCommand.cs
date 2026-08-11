using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;
using MediatR;

namespace ABP.Application.Features.CreditCards.Commands.CancelCreditCard;

public sealed record CancelCreditCardCommand(
    CancelCreditCardRequest Request) : IRequest<OperationResult>;
