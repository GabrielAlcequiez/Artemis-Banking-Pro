using ABP.Application.Features.Commerce.DTOs;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.Commerce.Queries.GetCommerces;

public sealed class GetCommercesQueryHandler(
    ICommerceRepository repository,
    IMapper mapper) : IRequestHandler<GetCommercesQuery, PagedResult<CommerceSummaryDto>>
{
    public async Task<PagedResult<CommerceSummaryDto>> Handle(
        GetCommercesQuery query,
        CancellationToken cancellationToken)
    {
        var request = query.Request;
        var readPage = await repository.SearchAsync(
            request.Page,
            request.PageSize,
            request.Status,
            cancellationToken);
        var data = mapper.Map<IReadOnlyCollection<CommerceSummaryDto>>(readPage.Data);

        return new(data, readPage.Page, readPage.PageSize, readPage.TotalRecords);
    }
}
