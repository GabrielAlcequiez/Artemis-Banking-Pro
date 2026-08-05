using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanPaymentService
{
    Task<OperationResult<LoanPaymentResult>> ProcessPaymentAsync(LoanPaymentRequest request, CancellationToken cancellationToken = default);
}
