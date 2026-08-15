using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.Application.Features.CreditCards.Services.Interfaces
{
    public interface ICardPaymentService
    {
        Task<ClientCardOperationOptions> GetClientOptionsAsync(
            CancellationToken cancellationToken = default);

        Task<OperationResult<CashierCardPaymentPreview>> PrepareCashierPaymentAsync(
            string sourceAccountNumber,
            string creditCardNumber,
            decimal amount,
            Guid operationId,
            CancellationToken cancellationToken = default);

        Task<CardOperationResult<FinancialOperationReceipt>> ProcessPaymentAsync(
            CreditCardPaymentRequest request,
            CancellationToken cancellationToken = default);
    }
}
