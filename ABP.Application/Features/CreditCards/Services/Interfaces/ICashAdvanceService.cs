using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.Application.Features.CreditCards.Services.Interfaces
{
    public interface ICashAdvanceService
    {
        Task<OperationResult<FinancialOperationReceipt>> ProcessCashAdvanceAsync(
            CashAdvanceRequest request,
            CancellationToken cancellationToken = default);
    }
}
