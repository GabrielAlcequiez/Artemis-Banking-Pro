using ABP.Application.Common;
using ABP.Application.Common.DTOs;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.CreditCards.DTOs;
using ABP.Application.Features.CreditCards.Services.Interfaces;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.CreditCards;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.CreditCards;
using ABP.Domain.ReadModels.Loans;

namespace ABP.Application.UnitTests.Dashboards;

internal sealed class DashboardUserRepository : IUserRepository
{
    public int ActiveClientCount { get; set; }

    public int InactiveClientCount { get; set; }

    public Task<User?> FindByIdentificationAsync(string identification) =>
        Task.FromResult<User?>(null);

    public Task<PagedResult<User>> GetPagedAsync(
        PagedRequest request,
        bool commerceOnly = false,
        Roles? role = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<User>([], request.Page, request.PageSize, 0));

    public Task<PagedResult<User>> GetActiveClientsPagedAsync(
        PagedRequest request,
        string? identification = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<User>([], request.Page, request.PageSize, 0));

    public Task<User?> GetActiveClientByIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveClientCount);

    public Task<int> CountInactiveClientsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(InactiveClientCount);

    public Task<bool> ExistsByCommerceIdAsync(
        Guid commerceId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public IQueryable<User> GetAllQueryable(bool trackChanges = false) =>
        Array.Empty<User>().AsQueryable();

    public Task<User?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);

    public Task<IReadOnlyList<User>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<User>>([]);

    public Task<User> AddAsync(
        User entity,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(entity);

    public Task<User?> UpdateAsync(
        string id,
        User value,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(value);

    public Task<User?> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<User?>(null);
}

internal sealed class DashboardSavingsAccountRepository : ISavingsAccountRepository
{
    public int ActiveAccountCount { get; set; }

    public IReadOnlyCollection<SavingsAccount> OwnedAccounts { get; set; } = [];

    public Task<SavingsAccount?> GetByAccountNumberAsync(
        string accountNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SavingsAccount?>(null);

    public Task<SavingsAccount?> GetPrincipalAccountAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SavingsAccount?>(OwnedAccounts.FirstOrDefault(account =>
            account.OwnerUserId == ownerUserId && account.Type == SavingsAccountType.Principal));

    public Task<bool> AccountNumberExistsAsync(
        string accountNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(OwnedAccounts);

    public Task<PagedResult<SavingsAccount>> GetPagedAsync(
        PagedRequest request,
        string? ownerIdentification = null,
        SavingsAccountStatus? status = null,
        SavingsAccountType? type = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<SavingsAccount>(
            [],
            request.Page,
            request.PageSize,
            status == SavingsAccountStatus.Active ? ActiveAccountCount : 0));

    public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) =>
        OwnedAccounts.AsQueryable();

    public Task<SavingsAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SavingsAccount?>(null);

    public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SavingsAccount>>(OwnedAccounts.ToArray());

    public Task<SavingsAccount> AddAsync(
        SavingsAccount entity,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(entity);

    public Task<SavingsAccount?> UpdateAsync(
        Guid id,
        SavingsAccount value,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SavingsAccount?>(value);

    public Task<SavingsAccount?> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SavingsAccount?>(null);
}

internal sealed class DashboardLoanRepository : ILoanRepository
{
    public int ActiveLoanCount { get; set; }

    public decimal TotalActiveDebt { get; set; }

    public Dictionary<string, decimal> ActiveDebtByClient { get; } =
        new(StringComparer.Ordinal);

    public Task<Loan?> GetByLoanNumberAsync(
        string loanNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(null);

    public Task<Loan?> GetWithInstallmentsAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(null);

    public Task<Loan?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(null);

    public Task<Loan?> GetDetailsForClientAsync(
        Guid id,
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(null);

    public Task<LoanPayment?> GetPaymentByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<LoanPayment?>(null);

    public Task<Loan?> GetActiveByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(null);

    public Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<ClientLoanPortfolioReadModel?>(null);

    public Task<bool> HasActiveLoanAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<decimal> GetActiveDebtByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveDebtByClient.GetValueOrDefault(clientId));

    public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(
        IReadOnlyCollection<string> clientIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, decimal>>(clientIds
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                clientId => clientId,
                clientId => ActiveDebtByClient.GetValueOrDefault(clientId),
                StringComparer.Ordinal));

    public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TotalActiveDebt);

    public Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(
        PagedRequest request,
        string? clientIdentification = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<LoanClientCandidateReadModel>([], request.Page, request.PageSize, 0));

    public Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<LoanClientCandidateReadModel?>(null);

    public Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveLoanCount);

    public Task<bool> LoanNumberExistsAsync(
        string loanNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(
        PagedRequest request,
        string? clientIdentification = null,
        LoanStatusFilter? status = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<LoanSummaryReadModel>([], request.Page, request.PageSize, 0));

    public Task<int> MarkOverdueInstallmentsAsync(
        DateOnly bankingDate,
        DateTimeOffset modifiedAtUtc,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<int> ClearLateFlagFromPaidInstallmentsAsync(
        Guid? loanId,
        DateTimeOffset modifiedAtUtc,
        string? modifiedByUserId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task AddInstallmentsAsync(
        IReadOnlyCollection<LoanInstallment> installments,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task AddPaymentAsync(
        LoanPayment payment,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public IQueryable<Loan> GetAllQueryable(bool trackChanges = false) =>
        Array.Empty<Loan>().AsQueryable();

    public Task<Loan?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(null);

    public Task<IReadOnlyList<Loan>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Loan>>([]);

    public Task<Loan> AddAsync(
        Loan entity,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(entity);

    public Task<Loan?> UpdateAsync(
        Guid id,
        Loan value,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(value);

    public Task<Loan?> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Loan?>(null);
}

internal sealed class DashboardCreditCardRepository : ICreditCardRepository
{
    public int ActiveCardCount { get; set; }

    public decimal TotalActiveDebt { get; set; }

    public Dictionary<string, decimal> ActiveDebtByClient { get; } =
        new(StringComparer.Ordinal);

    public Task<CreditCard?> GetByCardNumberAsync(
        string cardNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCard?>(null);

    public Task<bool> CardNumberExistsAsync(
        string cardNumber,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<CreditCard?> GetByCreationOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCard?>(null);

    public Task AddConsumptionAsync(
        CardConsumption consumption,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task AddPaymentAsync(
        CardPayment payment,
        CancellationToken cancellationToken = default) =>
        Task.CompletedTask;

    public Task<CardPayment?> GetPaymentByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CardPayment?>(null);

    public Task<CardConsumption?> GetConsumptionByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CardConsumption?>(null);

    public Task<IReadOnlyCollection<CreditCard>> GetActiveByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<CreditCard>>([]);

    public Task<string?> FindClientIdByIdentificationAsync(
        string identification,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<string?>(null);

    public Task<bool> HasAnyCardsAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    public Task<PagedResult<CreditCardSummaryReadModel>> SearchAsync(
        int page,
        int pageSize,
        string? identification = null,
        CreditCardStatusFilter? status = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<CreditCardSummaryReadModel>(
            [],
            page,
            pageSize,
            status == CreditCardStatusFilter.Active ? ActiveCardCount : 0));

    public Task<CreditCardDetailReadModel?> GetDetailsAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCardDetailReadModel?>(null);

    public Task<CreditCardDetailReadModel?> GetDetailsForClientAsync(
        Guid creditCardId,
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCardDetailReadModel?>(null);

    public Task<decimal> GetActiveDebtByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveDebtByClient.GetValueOrDefault(clientId));

    public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(
        IReadOnlyCollection<string> clientIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, decimal>>(clientIds
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(
                clientId => clientId,
                clientId => ActiveDebtByClient.GetValueOrDefault(clientId),
                StringComparer.Ordinal));

    public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(TotalActiveDebt);

    public Task<bool> IsActiveClientAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<bool> ClientExistsAsync(
        string clientId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(true);

    public Task<CreditCard?> GetForUpdateAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCard?>(null);

    public IQueryable<CreditCard> GetAllQueryable(bool trackChanges = false) =>
        Array.Empty<CreditCard>().AsQueryable();

    public Task<CreditCard?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCard?>(null);

    public Task<IReadOnlyList<CreditCard>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<CreditCard>>([]);

    public Task<CreditCard> AddAsync(
        CreditCard entity,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(entity);

    public Task<CreditCard?> UpdateAsync(
        Guid id,
        CreditCard value,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCard?>(value);

    public Task<CreditCard?> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<CreditCard?>(null);
}

internal sealed class DashboardTransactionRepository : IAccountTransactionRepository
{
    public int TotalCount { get; set; }

    public int TodayCount { get; set; }

    public int TotalPaymentCount { get; set; }

    public int TodayPaymentCount { get; set; }

    public List<DateOnly> RequestedDates { get; } = [];

    public Task<PagedResult<AccountTransaction>> GetPagedByAccountAsync(
        Guid accountId,
        PagedRequest request,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PagedResult<AccountTransaction>([], request.Page, request.PageSize, 0));

    public Task<IReadOnlyCollection<AccountTransaction>> GetByOperationIdAsync(
        Guid operationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<AccountTransaction>>([]);

    public Task<IReadOnlyCollection<AccountTransaction>> GetMostRecentByAccountAsync(
        Guid accountId,
        int count,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<AccountTransaction>>([]);

    public Task<IReadOnlyCollection<AccountTransaction>> GetAllByAccountAsync(
        Guid accountId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyCollection<AccountTransaction>>([]);

    public Task<int> CountByActorTodayAsync(
        string actorUserId,
        DateOnly today,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<int> CountByActorAndTypesTodayAsync(
        string actorUserId,
        DateOnly today,
        IReadOnlyCollection<FinancialOperationType> types,
        TransactionDirection? direction = null,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0);

    public Task<decimal> SumAmountByActorTodayAsync(
        string actorUserId,
        DateOnly today,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(0m);

    public Task<int> CountAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TotalCount);

    public Task<int> CountByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        RequestedDates.Add(date);
        return Task.FromResult(TodayCount);
    }

    public Task<int> CountPaymentsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(TotalPaymentCount);

    public Task<int> CountPaymentsByDateAsync(
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        RequestedDates.Add(date);
        return Task.FromResult(TodayPaymentCount);
    }

    public IQueryable<AccountTransaction> GetAllQueryable(bool trackChanges = false) =>
        Array.Empty<AccountTransaction>().AsQueryable();

    public Task<AccountTransaction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccountTransaction?>(null);

    public Task<IReadOnlyList<AccountTransaction>> GetAllAsync(
        bool trackChanges = false,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AccountTransaction>>([]);

    public Task<AccountTransaction> AddAsync(
        AccountTransaction entity,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(entity);

    public Task<AccountTransaction?> UpdateAsync(
        Guid id,
        AccountTransaction value,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccountTransaction?>(value);

    public Task<AccountTransaction?> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AccountTransaction?>(null);
}

internal sealed class DashboardClock(DateOnly today) : IClock
{
    public DateTimeOffset UtcNow => new(today, new TimeOnly(12, 0), TimeSpan.Zero);

    public DateTimeOffset Now => UtcNow;

    public DateOnly Today => today;
}

internal sealed class DashboardCurrentUser : ICurrentUserService
{
    public bool IsAuthenticated { get; init; }

    public string? UserId { get; init; }

    public string? UserName { get; init; }

    public Guid? CommerceId { get; init; }

    public IReadOnlyCollection<string> Roles { get; init; } = [];

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.Ordinal);
}

internal sealed class DashboardLoanService : ILoanService
{
    public ClientLoanPortfolioItemDto? ActiveLoan { get; init; }

    public Task<PagedResult<LoanSummaryDto>> ListAsync(
        LoanListRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<LoanDetailDto?> GetDetailAsync(
        Guid loanId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<LoanDetailDto?> GetClientDetailAsync(
        Guid loanId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<ClientLoanPortfolioItemDto?> GetClientActiveLoanAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveLoan);
}

internal sealed class DashboardCreditCardService : ICreditCardService
{
    public IReadOnlyCollection<ClientCreditCardPortfolioItemDto> ActiveCards { get; init; } = [];

    public Task<CreditCardListResult> ListAsync(
        CreditCardListRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CreditCardDetailDto?> GetDetailAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CreditCardDetailDto?> GetClientDetailAsync(
        Guid creditCardId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyCollection<ClientCreditCardPortfolioItemDto>> GetClientActiveCardsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult(ActiveCards);

    public Task<CardOperationResult<Guid>> CreateAsync(
        CreateCreditCardRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<CardOperationResult> UpdateLimitAsync(
        UpdateCreditLimitRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<OperationResult> CancelAsync(
        CancelCreditCardRequest request,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}
