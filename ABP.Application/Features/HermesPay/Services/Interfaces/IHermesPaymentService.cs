using ABP.Application.Common;
using ABP.Application.Features.HermesPay.DTOs;

namespace ABP.Application.Interfaces.Services
{
    public interface IHermesPaymentService
    {
        Task<OperationResult<FinancialOperationReceipt>> ProcessHermesPaymentAsync(
            ProcessHermesPaymentRequest request,
            CancellationToken cancellationToken = default);
    }
}
