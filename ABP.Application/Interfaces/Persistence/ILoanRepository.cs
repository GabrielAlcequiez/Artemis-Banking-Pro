using ABP.Application.Common;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;

namespace ABP.Application.Interfaces.Persistence;

public interface ILoanRepository : IGenericRepository<Loan, Guid>
{
    Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default);
    Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default);
    Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default);
    Task<PagedResult<Loan>> GetPagedAsync(PagedRequest request, string? clientIdentification = null, LoanStatus? status = null, CancellationToken cancellationToken = default);
    Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default);
}
