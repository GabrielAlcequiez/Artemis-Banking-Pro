using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Interfaces.Services;

public interface ILoanRateService
{
    Task<OperationResult> UpdateRateAsync(UpdateLoanRateRequest request, CancellationToken cancellationToken = default);
}
