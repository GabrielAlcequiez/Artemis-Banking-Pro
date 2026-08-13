using ABP.Domain.Common;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.ReadModels.Loans;

namespace ABP.Domain.Interfaces;

public interface ILoanRepository : IGenericRepository<Loan, Guid>
{
    Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default);
    Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Loan?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Loan?> GetDetailsForClientAsync(Guid id, string clientId, CancellationToken cancellationToken = default);
    Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default);
    Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(string clientId, CancellationToken cancellationToken = default);
    Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default);
    Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default);
    Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(PagedRequest request, string? clientIdentification = null, CancellationToken cancellationToken = default);
    Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default);
    Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default);
    Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(PagedRequest request, string? clientIdentification = null, LoanStatusFilter? status = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<LoanInstallment>> GetInstallmentsForDelinquencyUpdateAsync(DateOnly bankingDate, CancellationToken cancellationToken = default);
    Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default);
    Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default);
}
