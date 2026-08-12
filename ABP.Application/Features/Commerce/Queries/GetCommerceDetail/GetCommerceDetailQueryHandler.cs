using ABP.Application.Features.Commerce.DTOs;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.Commerce.Queries.GetCommerceDetail;

public sealed class GetCommerceDetailQueryHandler(
    ICommerceRepository repository,
    IMapper mapper) : IRequestHandler<GetCommerceDetailQuery, CommerceDetailDto?>
{
    public async Task<CommerceDetailDto?> Handle(
        GetCommerceDetailQuery query,
        CancellationToken cancellationToken)
    {
        var readModel = await repository.GetDetailsAsync(
            query.CommerceId,
            cancellationToken);

        return readModel is null
            ? null
            : mapper.Map<CommerceDetailDto>(readModel);
    }
}
