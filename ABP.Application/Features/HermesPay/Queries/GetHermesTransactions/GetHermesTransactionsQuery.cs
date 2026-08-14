using ABP.Application.Common;
using ABP.Application.Features.HermesPay.DTOs;
using MediatR;

namespace ABP.Application.Features.HermesPay.Queries.GetHermesTransactions;

public sealed record GetHermesTransactionsQuery(
    Guid RequestedCommerceId,
    int Page,
    int PageSize) : IRequest<OperationResult<HermesTransactionsPageDto>>;
