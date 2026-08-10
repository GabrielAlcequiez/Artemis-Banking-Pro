using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;
using MediatR;

namespace ABP.Application.Features.CreditCards.Commands.CreateCreditCard;

public sealed record CreateCreditCardCommand(
    CreateCreditCardRequest Request) : IRequest<OperationResult<Guid>>;
