using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;
using MediatR;

namespace ABP.Application.Features.Loans.Queries.AssessLoanRisk;

public sealed record AssessLoanRiskQuery(
    CreateLoanRequest Request)
    : IRequest<OperationResult<HighRiskAssessmentDto>>;
