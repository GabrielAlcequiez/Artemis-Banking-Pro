using ABP.Application.Features.CreditCards.DTOs;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.CreditCards.Queries.GetCreditCardDetail;

public sealed class GetCreditCardDetailQueryHandler(
    ICreditCardRepository repository,
    IMapper mapper) : IRequestHandler<GetCreditCardDetailQuery, CreditCardDetailDto?>
{
    public async Task<CreditCardDetailDto?> Handle(
        GetCreditCardDetailQuery query,
        CancellationToken cancellationToken)
    {
        var readModel = await repository.GetDetailsAsync(
            query.CreditCardId,
            cancellationToken);

        return readModel is null
            ? null
            : mapper.Map<CreditCardDetailDto>(readModel);
    }
}
