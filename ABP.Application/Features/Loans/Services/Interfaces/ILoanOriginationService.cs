using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanOriginationService
{
    Task<OperationResult<HighRiskAssessmentDto>> AssessRiskAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<LoanDetailDto>> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);
}
