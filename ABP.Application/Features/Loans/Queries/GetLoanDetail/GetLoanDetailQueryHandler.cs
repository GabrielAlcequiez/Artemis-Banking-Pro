using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Interfaces;
using AutoMapper;
using MediatR;

namespace ABP.Application.Features.Loans.Queries.GetLoanDetail;

public sealed class GetLoanDetailQueryHandler(
    ILoanRepository repository,
    IMapper mapper) : IRequestHandler<GetLoanDetailQuery, LoanDetailDto?>
{
    public async Task<LoanDetailDto?> Handle(
        GetLoanDetailQuery query,
        CancellationToken cancellationToken)
    {
        var loan = await repository.GetDetailsAsync(
            query.LoanId,
            cancellationToken);

        return loan is null
            ? null
            : mapper.Map<LoanDetailDto>(loan);
    }
}
