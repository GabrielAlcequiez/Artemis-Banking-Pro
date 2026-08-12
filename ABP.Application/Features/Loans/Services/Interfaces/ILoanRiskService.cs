using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanRiskService
{
    Task<HighRiskAssessmentDto> AssessAsync(CreateLoanRequest request, CancellationToken cancellationToken = default);
}
