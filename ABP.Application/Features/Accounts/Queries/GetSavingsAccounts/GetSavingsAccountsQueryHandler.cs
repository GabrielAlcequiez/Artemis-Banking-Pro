using ABP.Application.Features.Accounts.DTOs;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.Accounts.Queries.GetSavingsAccounts;

public sealed class GetSavingsAccountsQueryHandler(ISavingsAccountRepository accounts, IMapper mapper)
    : IRequestHandler<GetSavingsAccountsQuery, PagedResult<SavingsAccountSummaryDto>>
{
    public async Task<PagedResult<SavingsAccountSummaryDto>> Handle(
        GetSavingsAccountsQuery query, CancellationToken cancellationToken)
    {
        var page = await accounts.GetPagedAsync(
            query.PagedRequest, query.OwnerIdentification, query.Status, query.Type, cancellationToken);

        var data = mapper.Map<IReadOnlyCollection<SavingsAccountSummaryDto>>(page.Data);

        return new PagedResult<SavingsAccountSummaryDto>(data, page.Page, page.PageSize, page.TotalRecords);
    }
}
