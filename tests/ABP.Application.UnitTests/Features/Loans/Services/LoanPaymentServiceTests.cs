using ABP.Application.Common;
using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Accounts.Services.Interfaces;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Loans.Services;

public sealed class LoanPaymentServiceTests
{
    [Fact]
    public async Task GetClientOptionsAsync_returns_only_the_clients_active_products()
    {
        var dependencies = CreateDependencies(Roles.Client, "client-1");
        dependencies.Loans.Loan = CreateLoan(CreateInstallment(1, 100m));
        var service = dependencies.CreateService();

        var result = await service.GetClientOptionsAsync();

        var loan = Assert.Single(result.Loans);
        var account = Assert.Single(result.SavingsAccounts);
        Assert.Equal("client-1", dependencies.Loans.ReceivedClientId);
        Assert.Equal("client-1", dependencies.Accounts.ReceivedOwnerId);
        Assert.Equal("123456789", loan.LoanNumber);
        Assert.Equal(100m, loan.PendingAmount);
        Assert.Equal("987654321", account.AccountNumber);
    }

    [Fact]
    public async Task PrepareCashierPaymentAsync_allows_different_owners_and_caps_amount()
    {
        var dependencies = CreateDependencies(
            Roles.Cashier,
            "cashier-1",
            accountOwnerId: "account-client");
        dependencies.Loans.Loan = CreateLoan(CreateInstallment(1, 100m));
        var service = dependencies.CreateService();

        var result = await service.PrepareCashierPaymentAsync(
            " 987654321 ",
            " 123456789 ",
            500m,
            Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal("Cuenta Titular", result.Value.AccountOwnerFullName);
        Assert.Equal("Préstamo Titular", result.Value.LoanOwnerFullName);
        Assert.Equal(500m, result.Value.RequestedAmount);
        Assert.Equal(100m, result.Value.EffectiveAmount);
        Assert.Equal("987654321", dependencies.Accounts.ReceivedAccountNumber);
        Assert.Equal("123456789", dependencies.Loans.ReceivedLoanNumber);
    }

    [Fact]
    public async Task PrepareCashierPaymentAsync_rejects_non_cashier_before_querying()
    {
        var dependencies = CreateDependencies(Roles.Client, "client-1");
        var service = dependencies.CreateService();

        var result = await service.PrepareCashierPaymentAsync(
            "987654321",
            "123456789",
            50m,
            Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.CashierRequired, result.Error);
        Assert.Null(dependencies.Accounts.ReceivedAccountNumber);
        Assert.Null(dependencies.Loans.ReceivedLoanNumber);
    }

    [Fact]
    public async Task ProcessPaymentAsync_applies_partial_payment_to_oldest_installment()
    {
        var dependencies = CreateDependencies(
            Roles.Client,
            "client-1");
        var loan = CreateLoan(
            CreateInstallment(1, 100m, isLate: true),
            CreateInstallment(2, 100m));
        dependencies.Loans.Loan = loan;
        var service = dependencies.CreateService();

        var result = await service.ProcessPaymentAsync(
            CreateRequest(50m));

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, loan.Installments.Single(item => item.Number == 1).PendingAmount);
        Assert.Equal(
            InstallmentPaymentStatus.PartiallyPaid,
            loan.Installments.Single(item => item.Number == 1).PaymentStatus);
        Assert.True(loan.Installments.Single(item => item.Number == 1).IsLate);
        Assert.Equal(100m, loan.Installments.Single(item => item.Number == 2).PendingAmount);
        Assert.Equal(150m, loan.PendingAmount);
        Assert.Equal(50m, dependencies.Balance.DebitedAmount);
        Assert.Equal(1, dependencies.Ledger.ApprovedCalls);
        Assert.Equal(1, dependencies.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task ProcessPaymentAsync_caps_overpayment_and_completes_all_installments()
    {
        var dependencies = CreateDependencies(
            Roles.Cashier,
            "cashier-1",
            accountOwnerId: "different-client");
        var loan = CreateLoan(
            CreateInstallment(1, 75m, isLate: true),
            CreateInstallment(2, 25m));
        dependencies.Loans.Loan = loan;
        var service = dependencies.CreateService();

        var result = await service.ProcessPaymentAsync(
            CreateRequest(500m));

        Assert.True(result.IsSuccess);
        Assert.Equal(100m, result.Value.EffectiveAmount);
        Assert.Equal(100m, dependencies.Balance.DebitedAmount);
        Assert.All(loan.Installments, installment =>
        {
            Assert.Equal(0m, installment.PendingAmount);
            Assert.Equal(InstallmentPaymentStatus.Paid, installment.PaymentStatus);
            Assert.False(installment.IsLate);
        });
        Assert.Equal(0m, loan.PendingAmount);
        Assert.Equal(LoanStatus.Completed, loan.Status);
        Assert.True(result.Value.IsCompleted);
    }

    [Fact]
    public async Task ProcessPaymentAsync_rejects_client_that_does_not_own_loan()
    {
        var dependencies = CreateDependencies(
            Roles.Client,
            "other-client");
        dependencies.Loans.Loan = CreateLoan(
            CreateInstallment(1, 100m));
        var service = dependencies.CreateService();

        var result = await service.ProcessPaymentAsync(
            CreateRequest(50m));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.LoanOwnershipRequired, result.Error);
        Assert.Equal(0, dependencies.Balance.DebitCalls);
    }

    [Fact]
    public async Task ProcessPaymentAsync_returns_previous_payment_without_new_debit()
    {
        var request = CreateRequest(50m);
        var dependencies = CreateDependencies(
            Roles.Client,
            "client-1");
        dependencies.Loans.PreviousPayment = new LoanPayment
        {
            LoanId = request.LoanId,
            SourceAccountId = request.SourceAccountId,
            EffectiveAmount = 50m,
            ActorUserId = "client-1",
            PaidAtUtc = new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero),
            OperationId = request.OperationId,
            Loan = CreateLoan(CreateInstallment(1, 50m))
        };
        var service = dependencies.CreateService();

        var result = await service.ProcessPaymentAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(50m, result.Value.EffectiveAmount);
        Assert.Equal(0, dependencies.Balance.DebitCalls);
        Assert.Null(dependencies.Loans.AddedPayment);
        Assert.Equal(0, dependencies.Ledger.ApprovedCalls);
    }

    [Fact]
    public async Task ProcessPaymentAsync_rejects_reused_operation_for_other_payment()
    {
        var request = CreateRequest(50m);
        var dependencies = CreateDependencies(
            Roles.Client,
            "client-1");
        dependencies.Loans.PreviousPayment = new LoanPayment
        {
            LoanId = Guid.NewGuid(),
            SourceAccountId = request.SourceAccountId,
            EffectiveAmount = 50m,
            ActorUserId = "client-1",
            PaidAtUtc = new DateTimeOffset(2026, 8, 10, 10, 0, 0, TimeSpan.Zero),
            OperationId = request.OperationId,
            Loan = CreateLoan(CreateInstallment(1, 50m))
        };
        var service = dependencies.CreateService();

        var result = await service.ProcessPaymentAsync(request);

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.OperationConflict, result.Error);
        Assert.Equal(0, dependencies.Balance.DebitCalls);
    }

    [Fact]
    public async Task ProcessPaymentAsync_records_rejection_without_mutating_loan()
    {
        var dependencies = CreateDependencies(
            Roles.Client,
            "client-1");
        var loan = CreateLoan(CreateInstallment(1, 100m));
        dependencies.Loans.Loan = loan;
        var debitError = new Error(
            "accounts.insufficient_funds",
            "Fondos insuficientes.");
        dependencies.Balance.Result = OperationResult.Failure(debitError);
        var service = dependencies.CreateService();

        var result = await service.ProcessPaymentAsync(
            CreateRequest(50m));

        Assert.True(result.IsFailure);
        Assert.Equal(debitError, result.Error);
        Assert.Equal(100m, loan.PendingAmount);
        Assert.Equal(100m, loan.Installments.Single().PendingAmount);
        Assert.Null(dependencies.Loans.AddedPayment);
        Assert.Equal(1, dependencies.Ledger.RejectedCalls);
        Assert.Equal(0, dependencies.UnitOfWork.SaveCalls);
    }

    private static LoanPaymentRequest CreateRequest(decimal amount) =>
        new(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            amount,
            Guid.Parse("33333333-3333-3333-3333-333333333333"));

    private static Loan CreateLoan(params LoanInstallment[] installments)
    {
        var loan = new Loan
        {
            ClientId = "client-1",
            LoanNumber = "123456789",
            Capital = 200m,
            PendingAmount = installments.Sum(item => item.PendingAmount),
            AnnualInterestRate = 12m,
            TermInMonths = 12,
            Status = LoanStatus.Active,
            AssignedByUserId = "admin-1",
            Installments = installments
        };

        foreach (var installment in installments)
        {
            installment.Loan = loan;
        }

        return loan;
    }

    private static LoanInstallment CreateInstallment(
        int number,
        decimal pendingAmount,
        bool isLate = false) =>
        new()
        {
            Number = number,
            DueDate = new DateOnly(2026, 9, 11).AddMonths(number - 1),
            InstallmentAmount = 100m,
            InterestAmount = 10m,
            CapitalAmount = 90m,
            PendingAmount = pendingAmount,
            PaymentStatus = InstallmentPaymentStatus.Pending,
            IsLate = isLate
        };

    private static Dependencies CreateDependencies(
        Roles role,
        string userId,
        string accountOwnerId = "client-1") =>
        new()
        {
            Loans = new StubLoanRepository(),
            Accounts = new StubSavingsAccountRepository
            {
                Account = new SavingsAccount(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"))
                {
                    OwnerUserId = accountOwnerId,
                    AccountNumber = "987654321",
                    Balance = 1_000m,
                    Status = SavingsAccountStatus.Active,
                    Type = SavingsAccountType.Principal
                }
            },
            Balance = new StubAccountBalanceService(),
            Ledger = new StubAccountLedger(),
            UnitOfWork = new StubUnitOfWork(),
            CurrentUser = new StubCurrentUser(role, userId),
            Users = new StubUserRepository()
        };

    private sealed class Dependencies
    {
        public required StubLoanRepository Loans { get; init; }
        public required StubSavingsAccountRepository Accounts { get; init; }
        public required StubAccountBalanceService Balance { get; init; }
        public required StubAccountLedger Ledger { get; init; }
        public required StubUnitOfWork UnitOfWork { get; init; }
        public required StubCurrentUser CurrentUser { get; init; }
        public required StubUserRepository Users { get; init; }

        public LoanPaymentService CreateService() =>
            new(
                Loans,
                Accounts,
                Users,
                Balance,
                Ledger,
                UnitOfWork,
                CurrentUser,
                new StubClock(),
                new LoanPaymentRequestValidator(),
                NullLogger<LoanPaymentService>.Instance);
    }

    private sealed class StubCurrentUser(Roles role, string userId)
        : ICurrentUserService
    {
        public bool IsAuthenticated => true;
        public string? UserId => userId;
        public string? UserName => userId;
        public Guid? CommerceId => null;
        public IReadOnlyCollection<string> Roles => [role.ToString()];
        public bool IsInRole(string requestedRole) => Roles.Contains(requestedRole);
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow => new(2026, 8, 11, 12, 0, 0, TimeSpan.Zero);
        public DateTimeOffset Now => UtcNow;
        public DateOnly Today => DateOnly.FromDateTime(Now.DateTime);
    }

    private sealed class StubAccountBalanceService : IAccountBalanceService
    {
        public OperationResult Result { get; set; } = OperationResult.Success();
        public decimal? DebitedAmount { get; private set; }
        public int DebitCalls { get; private set; }

        public Task<OperationResult> DebitAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default)
        {
            DebitedAmount = amount;
            DebitCalls++;
            return Task.FromResult(Result);
        }

        public Task<OperationResult> CreditAsync(Guid accountId, decimal amount, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubAccountLedger : IAccountLedger
    {
        public int ApprovedCalls { get; private set; }
        public int RejectedCalls { get; private set; }

        public Task RecordApprovedAsync(Guid operationId, Guid accountId, decimal amount, TransactionDirection direction, FinancialOperationType operationType, string? origin, string? beneficiary, string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            ApprovedCalls++;
            return Task.CompletedTask;
        }

        public Task RecordRejectedAsync(Guid accountId, Guid operationId, decimal amount, TransactionDirection direction, FinancialOperationType operationType, string rejectionReason, string? actorUserId, string? actorRole, CancellationToken cancellationToken = default)
        {
            RejectedCalls++;
            return Task.CompletedTask;
        }
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
        public Loan? Loan { get; set; }
        public LoanPayment? PreviousPayment { get; set; }
        public LoanPayment? AddedPayment { get; private set; }
        public string? ReceivedClientId { get; private set; }
        public string? ReceivedLoanNumber { get; private set; }
        public Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Loan);
        public Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => Task.FromResult(PreviousPayment);
        public Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default)
        {
            AddedPayment = payment;
            return Task.CompletedTask;
        }
        public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default)
        {
            ReceivedLoanNumber = loanNumber;
            return Task.FromResult(Loan);
        }
        public Task<Loan?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetDetailsForClientAsync(Guid id, string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            ReceivedClientId = clientId;
            return Task.FromResult(Loan);
        }
        public Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(PagedRequest request, string? clientIdentification = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(PagedRequest request, string? clientIdentification = null, LoanStatusFilter? status = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyCollection<LoanInstallment>> GetInstallmentsForDelinquencyUpdateAsync(DateOnly bankingDate, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<Loan> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<IReadOnlyList<Loan>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan> AddAsync(Loan entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> UpdateAsync(Guid id, Loan value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubSavingsAccountRepository : ISavingsAccountRepository
    {
        public SavingsAccount? Account { get; init; }
        public string? ReceivedOwnerId { get; private set; }
        public string? ReceivedAccountNumber { get; private set; }
        public Task<SavingsAccount?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(Account);
        public Task<IReadOnlyCollection<SavingsAccount>> GetActiveByOwnerIdAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            ReceivedOwnerId = ownerUserId;
            IReadOnlyCollection<SavingsAccount> accounts = Account is null ? [] : [Account];
            return Task.FromResult(accounts);
        }
        public Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber, CancellationToken cancellationToken = default)
        {
            ReceivedAccountNumber = accountNumber;
            return Task.FromResult(Account);
        }
        public Task<SavingsAccount?> GetPrincipalAccountAsync(string ownerUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> AccountNumberExistsAsync(string accountNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<SavingsAccount>> GetPagedAsync(PagedRequest request, string? ownerIdentification = null, SavingsAccountStatus? status = null, SavingsAccountType? type = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount> AddAsync(SavingsAccount entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<SavingsAccount>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<SavingsAccount> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<SavingsAccount?> UpdateAsync(Guid id, SavingsAccount value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<SavingsAccount?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }

    private sealed class StubUserRepository : IUserRepository
    {
        public Task<User?> GetByIdAsync(
            string id,
            CancellationToken cancellationToken = default)
        {
            var user = new User(id)
            {
                Name = id == "account-client" ? "Cuenta" : "Préstamo",
                LastName = "Titular"
            };
            return Task.FromResult<User?>(user);
        }

        public Task<User?> FindByIdentificationAsync(string identification) => throw new NotImplementedException();
        public Task<PagedResult<User>> GetPagedAsync(PagedRequest request, bool commerceOnly = false, Roles? role = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<PagedResult<User>> GetActiveClientsPagedAsync(PagedRequest request, string? identification = null, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> GetActiveClientByIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountActiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> CountInactiveClientsAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<bool> ExistsByCommerceIdAsync(Guid commerceId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<User>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<User> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<User> AddAsync(User entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> UpdateAsync(string id, User value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<User?> DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
