using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetAccountTransactions;

public sealed record GetAccountTransactionsQuery(
    Guid AccountId, PagedRequest PagedRequest) : IRequest<PagedResult<AccountTransactionDto>>;
