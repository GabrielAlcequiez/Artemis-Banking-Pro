using ABP.Application.Features.Loans.DTOs;
using ABP.Domain.Common;

namespace ABP.Application.Features.Loans.Services.Interfaces;

public interface ILoanService
{
    Task<PagedResult<LoanSummaryDto>> ListAsync(LoanListRequest request, CancellationToken cancellationToken = default);
    Task<LoanDetailDto?> GetDetailAsync(Guid loanId, CancellationToken cancellationToken = default);
}
