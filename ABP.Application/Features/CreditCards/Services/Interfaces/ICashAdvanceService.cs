using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.Application.Features.CreditCards.Services.Interfaces
{
    public interface ICashAdvanceService
    {
        Task<ClientCardOperationOptions> GetClientOptionsAsync(
            CancellationToken cancellationToken = default);

        Task<CardOperationResult<FinancialOperationReceipt>> ProcessCashAdvanceAsync(
            CashAdvanceRequest request,
            CancellationToken cancellationToken = default);
    }
}
