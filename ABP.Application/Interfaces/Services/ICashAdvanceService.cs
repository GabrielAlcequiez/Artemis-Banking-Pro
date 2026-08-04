using ABP.Application.Common;
using ABP.Application.Features.CreditCards.DTOs;

namespace ABP.Application.Interfaces.Services
{
    public interface ICashAdvanceService
    {
        Task<OperationResult<FinancialOperationReceipt>> ProcessCashAdvanceAsync(
            CashAdvanceRequest request,
            CancellationToken cancellationToken = default);
    }
}
