using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Mapping;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;
using ABP.Application.UnitTests.Features.Loans;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Loans.Services;

public sealed class LoanOriginationServiceTests
{
    [Fact]
    public async Task CreateAsync_originates_loan_installments_and_disbursement()
    {
        var dependencies = CreateDependencies();
        var service = dependencies.CreateService();

        var result = await service.CreateAsync(CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.NotNull(dependencies.Loans.AddedLoan);
        Assert.Equal("123456789", dependencies.Loans.AddedLoan.LoanNumber);
        Assert.Equal(12, dependencies.Loans.AddedLoan.Installments.Count);
        Assert.Equal(
            dependencies.Loans.AddedLoan.Installments.Sum(item => item.PendingAmount),
            dependencies.Loans.AddedLoan.PendingAmount);
        Assert.Equal(10_000m, dependencies.Balance.ReceivedAmount);
        Assert.Equal(
            dependencies.Accounts.PrincipalAccount!.Id,
            dependencies.Balance.ReceivedAccountId);
        Assert.Equal(FinancialOperationType.LoanDisbursement, dependencies.Ledger.OperationType);
        Assert.Equal(1, dependencies.UnitOfWork.SaveCalls);
        Assert.Equal("123456789", result.Value.LoanNumber);
        Assert.Equal(12, result.Value.Amortization.Count);
        Assert.False(result.HasNotificationWarning);
        var email = Assert.Single(dependencies.Emails.SentEmails);
        Assert.Equal("ana@example.com", email.ToEmail);
        Assert.Equal("Préstamo aprobado", email.Subject);
        Assert.Contains("123456789", email.Body);
        Assert.False(dependencies.Emails.WasCalledBeforeCommit);
    }

    [Fact]
    public async Task CreateAsync_keeps_loan_confirmed_when_email_fails()
    {
        var dependencies = CreateDependencies();
        dependencies.Emails.ThrowOnSend = true;
        var service = dependencies.CreateService();

        var result = await service.CreateAsync(CreateRequest());

        Assert.True(result.IsSuccess);
        Assert.True(result.HasNotificationWarning);
        Assert.Equal(1, dependencies.UnitOfWork.SaveCalls);
        Assert.NotNull(dependencies.Loans.AddedLoan);
        Assert.Equal(1, dependencies.Emails.SendAttempts);
        Assert.False(dependencies.Emails.WasCalledBeforeCommit);
    }

    [Fact]
    public async Task CreateAsync_requires_explicit_confirmation_for_high_risk()
    {
        var dependencies = CreateDependencies();
        dependencies.Risk.Assessment = new HighRiskAssessmentDto(
            LoanRiskType.ProjectedHighRisk.ToString(),
            500m,
            11_000m,
            1_000m,
            true);
        var service = dependencies.CreateService();

        var result = await service.CreateAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.HighRiskConfirmationRequired, result.Error);
        Assert.Null(dependencies.Loans.AddedLoan);
        Assert.Equal(0, dependencies.Balance.CreditCalls);
        Assert.Equal(0, dependencies.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task CreateAsync_propagates_disbursement_failure_without_final_commit()
    {
        var dependencies = CreateDependencies();
        var disbursementError = new Error(
            "accounts.inactive_account",
            "La cuenta está inactiva.");
        dependencies.Balance.Result = OperationResult.Failure(
            disbursementError);
        var service = dependencies.CreateService();

        var result = await service.CreateAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(disbursementError, result.Error);
        Assert.NotNull(dependencies.Loans.AddedLoan);
        Assert.Equal(0, dependencies.Ledger.RecordCalls);
        Assert.Equal(0, dependencies.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task AssessRiskAsync_rejects_client_with_active_loan()
    {
        var dependencies = CreateDependencies();
        dependencies.Loans.HasActiveLoan = true;
        var service = dependencies.CreateService();

        var result = await service.AssessRiskAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.ActiveLoanExists, result.Error);
        Assert.Equal(0, dependencies.Risk.AssessCalls);
    }

    [Fact]
    public async Task CreateAsync_returns_controlled_error_when_identifier_generation_fails()
    {
        var dependencies = CreateDependencies();
        dependencies.Identifier.ShouldFail = true;
        var service = dependencies.CreateService();

        var result = await service.CreateAsync(CreateRequest());

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.NumberGenerationFailed, result.Error);
        Assert.Null(dependencies.Loans.AddedLoan);
        Assert.Equal(0, dependencies.Balance.CreditCalls);
        Assert.Equal(0, dependencies.UnitOfWork.SaveCalls);
    }

    private static CreateLoanRequest CreateRequest() =>
        new("client-1", 10_000m, 12, 12m);

    private static Dependencies CreateDependencies()
    {
        var accountId = Guid.NewGuid();

        return new Dependencies
        {
            Loans = new StubLoanRepository(),
            Users = new StubUserRepository
            {
                User = new User("client-1")
                {
                    Name = "Ana",
                    LastName = "Pérez",
                    Email = "ana@example.com",
                    Role = Roles.Client,
                    IsActive = true
                }
            },
            Accounts = new StubSavingsAccountRepository
            {
                PrincipalAccount = new SavingsAccount(accountId)
                {
                    OwnerUserId = "client-1",
                    AccountNumber = "987654321",
                    Status = SavingsAccountStatus.Active,
                    Type = SavingsAccountType.Principal
                }
            },
            Identifier = new StubIdentifierGenerator(),
            Balance = new StubAccountBalanceService(),
            Ledger = new StubAccountLedger(),
            Risk = new StubLoanRiskService(),
            UnitOfWork = new StubUnitOfWork(),
            Emails = new RecordingLoanEmailService()
        };
    }

    private sealed class Dependencies
    {
        public required StubLoanRepository Loans { get; init; }
        public required StubUserRepository Users { get; init; }
        public required StubSavingsAccountRepository Accounts { get; init; }
        public required StubIdentifierGenerator Identifier { get; init; }
        public required StubAccountBalanceService Balance { get; init; }
        public required StubAccountLedger Ledger { get; init; }
        public required StubLoanRiskService Risk { get; init; }
        public required StubUnitOfWork UnitOfWork { get; init; }
        public required RecordingLoanEmailService Emails { get; init; }

        public LoanOriginationService CreateService()
        {
            Emails.IsOperationCommitted = () => UnitOfWork.SaveCalls > 0;

            return new LoanOriginationService(
                Loans,
                Users,
                Accounts,
                Identifier,
                Balance,
                Ledger,
                Risk,
                new AmortizationCalculator(),
                UnitOfWork,
                new StubCurrentUser(),
                new StubClock(new DateOnly(2026, 8, 11)),
                new CreateLoanRequestValidator(),
                CreateMapper(),
                Emails,
                NullLogger<LoanOriginationService>.Instance);
        }

        private static IMapper CreateMapper() =>
            new MapperConfiguration(
                configuration => configuration.AddProfile<LoanProfile>(),
                NullLoggerFactory.Instance).CreateMapper();
    }

    private sealed class StubCurrentUser : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public string? UserId => "admin-1";
        public string? UserName => "Administrator";
        public Guid? CommerceId => null;
        public IReadOnlyCollection<string> Roles => [ABP.Domain.Enums.Roles.Administrator.ToString()];
        public bool IsInRole(string role) => Roles.Contains(role);
    }

    private sealed class StubClock(DateOnly today) : IClock
    {
        public DateTimeOffset UtcNow => new(today, new TimeOnly(12, 0), TimeSpan.Zero);
        public DateTimeOffset Now => UtcNow;
        public DateOnly Today => today;
    }

    private sealed class StubLoanRiskService : ILoanRiskService
    {
        public HighRiskAssessmentDto Assessment { get; set; } =
            new(LoanRiskType.None.ToString(), 0m, 10_661.88m, 20_000m, false);
        public int AssessCalls { get; private set; }

        public Task<HighRiskAssessmentDto> AssessAsync(
            CreateLoanRequest request,
            CancellationToken cancellationToken = default)
        {
            AssessCalls++;
            return Task.FromResult(Assessment);
        }
    }

    private sealed class StubIdentifierGenerator : IFinancialIdentifierGenerator
    {
        public bool ShouldFail { get; set; }

        public Task<string> GenerateNineDigitIdentifierAsync(
            FinancialIdentifierType type,
            CancellationToken cancellationToken = default)
        {
            if (ShouldFail)
            {
                throw new InvalidOperationException(
                    "No hay identificadores disponibles.");
            }

            return Task.FromResult("123456789");
        }
    }

    private sealed class StubAccountBalanceService : IAccountBalanceService
    {
        public OperationResult Result { get; set; } = OperationResult.Success();
        public Guid? ReceivedAccountId { get; private set; }
        public decimal? ReceivedAmount { get; private set; }
        public int CreditCalls { get; private set; }

        public Task<OperationResult> CreditAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            ReceivedAccountId = accountId;
            ReceivedAmount = amount;
            CreditCalls++;
            return Task.FromResult(Result);
        }

        public Task<OperationResult> DebitAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class StubAccountLedger : IAccountLedger
    {
        public int RecordCalls { get; private set; }
        public FinancialOperationType? OperationType { get; private set; }

        public Task RecordApprovedAsync(Guid operationId, Guid accountId, decimal amount, TransactionDirection direction, FinancialOperationType operationType, string? origin, string? beneficiary, string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            RecordCalls++;
            OperationType = operationType;
            return Task.CompletedTask;
        }

        public Task RecordRejectedAsync(Guid accountId, Guid operationId, decimal amount, TransactionDirection direction, FinancialOperationType operationType, string rejectionReason, string? actorUserId, string? actorRole, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class StubLoanRepository : ILoanRepository
    {
        public bool HasActiveLoan { get; set; }
        public Loan? AddedLoan { get; private set; }
        public Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default) => Task.FromResult(HasActiveLoan);
        public Task<Loan> AddAsync(Loan entity, CancellationToken cancellationToken = default)
        {
            AddedLoan = entity;
            return Task.FromResult(entity);
        }
        public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetDetailsForClientAsync(Guid id, string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(PagedRequest request, string? clientIdentification = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(PagedRequest request, string? clientIdentification = null, LoanStatusFilter? status = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> MarkOverdueInstallmentsAsync(DateOnly bankingDate, DateTimeOffset modifiedAtUtc, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> ClearLateFlagFromPaidInstallmentsAsync(Guid? loanId, DateTimeOffset modifiedAtUtc, string? modifiedByUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<Loan> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<IReadOnlyList<Loan>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> UpdateAsync(Guid id, Loan value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public User? User { get; init; }
        public Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default) => Task.FromResult(User);
        public Task<User?> FindByIdentificationAsync(string identification) => throw new NotImplementedException();
        public Task<PagedResult<User>> GetPagedAsync(PagedRequest request, bool commerceOnly = false, Roles? role = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<User>> GetActiveClientsPagedAsync(PagedRequest request, string? identification = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetActiveClientByIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountInactiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByCommerceIdAsync(Guid commerceId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<User>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<User> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<User?> UpdateAsync(string id, User value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubSavingsAccountRepository : ISavingsAccountRepository
    {
        public SavingsAccount? PrincipalAccount { get; init; }
        public Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default) => Task.FromResult(PrincipalAccount);
        public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(string ownerUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<SavingsAccount>> GetPagedAsync(PagedRequest request, string? ownerIdentification = null, SavingsAccountStatus? status = null, SavingsAccountType? type = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount> AddAsync(SavingsAccount entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<SavingsAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount?> UpdateAsync(Guid id, SavingsAccount value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
