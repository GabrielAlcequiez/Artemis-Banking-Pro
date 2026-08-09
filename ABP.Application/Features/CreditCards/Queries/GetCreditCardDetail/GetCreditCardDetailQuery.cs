using ABP.Application.Features.CreditCards.DTOs;
using MediatR;

namespace ABP.Application.Features.CreditCards.Queries.GetCreditCardDetail;

public sealed record GetCreditCardDetailQuery(
    Guid CreditCardId) : IRequest<CreditCardDetailDto?>;
