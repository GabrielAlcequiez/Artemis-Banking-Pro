using ABP.Application.Common;
using ABP.Application.Features.Loans.DTOs;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanPaymentService
{
    Task<ClientLoanPaymentOptions> GetClientOptionsAsync(CancellationToken cancellationToken = default);
    Task<OperationResult<CashierLoanPaymentPreview>> PrepareCashierPaymentAsync(string sourceAccountNumber, string loanNumber, decimal amount, Guid operationId, CancellationToken cancellationToken = default);
    Task<OperationResult<LoanPaymentResult>> ProcessPaymentAsync(LoanPaymentRequest request, CancellationToken cancellationToken = default);
}
