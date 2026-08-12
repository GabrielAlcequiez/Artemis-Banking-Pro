using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Common;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.Loans.Queries.GetLoans;

public sealed class GetLoansQueryHandler(
    ILoanRepository repository,
    IMapper mapper)
    : IRequestHandler<GetLoansQuery, PagedResult<LoanSummaryDto>>
{
    public async Task<PagedResult<LoanSummaryDto>> Handle(
        GetLoansQuery query,
        CancellationToken cancellationToken)
    {
        var request = query.Request;
        var identification = NormalizeIdentification(request.Identification);
        var page = await repository.GetPagedAsync(
            new PagedRequest(request.Page, request.PageSize),
            identification,
            request.Status,
            cancellationToken);
        var data = mapper.Map<IReadOnlyCollection<LoanSummaryDto>>(page.Data);

        return new PagedResult<LoanSummaryDto>(
            data,
            page.Page,
            page.PageSize,
            page.TotalRecords);
    }

    private static string? NormalizeIdentification(string? identification) =>
        string.IsNullOrWhiteSpace(identification)
            ? null
            : identification.Trim();
}
