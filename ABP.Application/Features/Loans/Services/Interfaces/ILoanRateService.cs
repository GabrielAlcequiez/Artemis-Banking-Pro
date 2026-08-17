using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanRateService
{
    Task<LoanOperationResult> UpdateRateAsync(UpdateLoanRateRequest request, CancellationToken cancellationToken = default);
}
