using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Interfaces.Services;

public interface ILoanPaymentService
{
    Task<OperationResult<LoanPaymentResult>> ProcessPaymentAsync(LoanPaymentRequest request, CancellationToken cancellationToken = default);
}
