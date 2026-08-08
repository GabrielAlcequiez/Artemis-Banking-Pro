using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.Application.Features.CreditCards.Services.Interfaces
{
    public interface ICardPaymentService
    {
        Task<OperationResult<FinancialOperationReceipt>> ProcessPaymentAsync(
            CreditCardPaymentRequest request,
            CancellationToken cancellationToken = default);
    }
}
