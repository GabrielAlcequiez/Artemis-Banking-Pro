using ABP.Application.Features.Commerce.DTOs;
using ABP.Domain.Common;
using MediatR;

namespace ABP.Application.Features.Commerce.Queries.GetCommerces;

public sealed record GetCommercesQuery(
    CommerceListRequest Request) : IRequest<PagedResult<CommerceSummaryDto>>;
