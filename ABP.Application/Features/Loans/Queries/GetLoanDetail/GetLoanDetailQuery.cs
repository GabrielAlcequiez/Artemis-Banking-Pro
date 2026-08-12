using ABP.Application.Features.Loans.DTOs;
using MediatR;

namespace ABP.Application.Features.Loans.Queries.GetLoanDetail;

public sealed record GetLoanDetailQuery(
    Guid LoanId) : IRequest<LoanDetailDto?>;
