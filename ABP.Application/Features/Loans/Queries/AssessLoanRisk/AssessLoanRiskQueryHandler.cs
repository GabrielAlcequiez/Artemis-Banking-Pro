using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using MediatR;

namespace ABP.Application.Features.Loans.Queries.AssessLoanRisk;

public sealed class AssessLoanRiskQueryHandler(
    ILoanOriginationService originationService)
    : IRequestHandler<AssessLoanRiskQuery, OperationResult<HighRiskAssessmentDto>>
{
    public Task<OperationResult<HighRiskAssessmentDto>> Handle(
        AssessLoanRiskQuery query,
        CancellationToken cancellationToken)
    {
        return originationService.AssessRiskAsync(
            query.Request,
            cancellationToken);
    }
}
