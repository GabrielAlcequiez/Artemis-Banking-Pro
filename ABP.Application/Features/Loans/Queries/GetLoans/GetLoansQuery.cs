using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Common;
using MediatR;

namespace ABP.Application.Features.Loans.Queries.GetLoans;

public sealed record GetLoansQuery(
    LoanListRequest Request) : IRequest<PagedResult<LoanSummaryDto>>;
