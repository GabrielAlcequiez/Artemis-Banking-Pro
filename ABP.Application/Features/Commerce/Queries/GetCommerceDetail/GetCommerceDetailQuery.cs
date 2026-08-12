using ABP.Application.Features.Commerce.DTOs;
using MediatR;

namespace ABP.Application.Features.Commerce.Queries.GetCommerceDetail;

public sealed record GetCommerceDetailQuery(
    Guid CommerceId) : IRequest<CommerceDetailDto?>;
