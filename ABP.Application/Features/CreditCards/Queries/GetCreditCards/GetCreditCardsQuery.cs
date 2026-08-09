using ABP.Application.Features.CreditCards.DTOs;
using MediatR;

namespace ABP.Application.Features.CreditCards.Queries.GetCreditCards;

public sealed record GetCreditCardsQuery(
    CreditCardListRequest Request) : IRequest<CreditCardListResult>;
