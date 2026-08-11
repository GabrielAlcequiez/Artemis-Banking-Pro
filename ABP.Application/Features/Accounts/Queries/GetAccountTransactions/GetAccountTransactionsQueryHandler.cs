using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetAccountTransactions;

public sealed class GetAccountTransactionsQueryHandler(IAccountTransactionRepository transactions, IMapper mapper)
    : IRequestHandler<GetAccountTransactionsQuery, PagedResult<AccountTransactionDto>>
{
    public async Task<PagedResult<AccountTransactionDto>> Handle(
        GetAccountTransactionsQuery query, CancellationToken cancellationToken)
    {
        var page = await transactions.GetPagedByAccountAsync(
            query.AccountId, query.PagedRequest, cancellationToken);

        var data = mapper.Map<IReadOnlyCollection<AccountTransactionDto>>(page.Data);

        return new PagedResult<AccountTransactionDto>(data, page.Page, page.PageSize, page.TotalRecords);
    }
}
