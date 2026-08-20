using System.Transactions;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Accounts;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Infrastructure.Persistence.Context;
using ABP.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infrastructure.IntegrationTests.Loans;

public sealed class LoanRepositoryTests : IAsyncLifetime
{
    #region Test setup

    private readonly string _databaseName = $"ABP_LoanRepoTests_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private AppDbContext _context = null!;
    private LoanRepository _repository = null!;

    public LoanRepositoryTests()
    {
        _connectionString = TestDatabase.CreateConnectionString(_databaseName);
    }

    public async Task InitializeAsync()
    {
        _context = CreateContext();
        await _context.Database.EnsureCreatedAsync();

        _repository = new LoanRepository(_context);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
    }

    #endregion

    #region Loan query tests

    [Fact]
    public async Task GetByLoanNumber_returns_existing_loan_without_tracking()
    {
        var seeded = await SeedAsync(_context);
        _context.ChangeTracker.Clear();

        var result = await _repository.GetByLoanNumberAsync(seeded.ActiveNew.LoanNumber);

        Assert.NotNull(result);
        Assert.Equal(seeded.ActiveNew.Id, result.Id);
        Assert.Empty(_context.ChangeTracker.Entries<Loan>());
    }

    [Fact]
    public async Task GetByLoanNumber_returns_null_when_loan_does_not_exist()
    {
        await SeedAsync(_context);

        var result = await _repository.GetByLoanNumberAsync("999999999");

        Assert.Null(result);
    }

    [Fact]
    public async Task LoanNumberExists_returns_expected_result()
    {
        var seeded = await SeedAsync(_context);

        var existingResult = await _repository.LoanNumberExistsAsync(seeded.ActiveOld.LoanNumber);
        var missingResult = await _repository.LoanNumberExistsAsync("999999999");

        Assert.True(existingResult);
        Assert.False(missingResult);
    }

    [Fact]
    public async Task GetActiveByClientId_returns_only_active_loan()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetActiveByClientIdAsync(seeded.ActiveNew.ClientId);

        Assert.NotNull(result);
        Assert.Equal(seeded.ActiveNew.Id, result.Id);
        Assert.Equal(LoanStatus.Active, result.Status);
    }

    [Fact]
    public async Task GetActivePortfolioForClient_projects_only_the_clients_active_loan()
    {
        var seeded = await SeedAsync(_context);
        _context.ChangeTracker.Clear();

        var result = await _repository.GetActivePortfolioForClientAsync(
            seeded.ActiveNew.ClientId);
        var clientWithoutLoan = await _repository.GetActivePortfolioForClientAsync(
            "client-without-loans");

        Assert.NotNull(result);
        Assert.Equal(seeded.ActiveNew.Id, result.Id);
        Assert.Equal(seeded.ActiveNew.LoanNumber, result.LoanNumber);
        Assert.Equal(10_000m, result.CapitalAmount);
        Assert.Equal(10_000m, result.PendingAmount);
        Assert.Equal(900m, result.MonthlyInstallment);
        Assert.Null(clientWithoutLoan);
        Assert.Empty(_context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task HasActiveLoan_returns_expected_result()
    {
        var seeded = await SeedAsync(_context);

        var activeResult = await _repository.HasActiveLoanAsync(seeded.ActiveNew.ClientId);
        var missingResult = await _repository.HasActiveLoanAsync("client-without-loans");

        Assert.True(activeResult);
        Assert.False(missingResult);
    }

    [Fact]
    public async Task CountActiveLoans_returns_only_active_loan_count()
    {
        await SeedAsync(_context);

        var result = await _repository.CountActiveLoansAsync();

        Assert.Equal(2, result);
    }

    [Fact]
    public async Task GetWithInstallments_returns_ordered_installments_and_tracks_loan()
    {
        var seeded = await SeedAsync(_context);
        _context.ChangeTracker.Clear();

        var result = await _repository.GetWithInstallmentsAsync(seeded.ActiveNew.Id);

        Assert.NotNull(result);
        Assert.Equal("María", result.Client.Name);
        Assert.Equal([1, 2], result.Installments.Select(x => x.Number).ToArray());
        Assert.Equal(EntityState.Unchanged, _context.Entry(result).State);
    }

    [Fact]
    public async Task GetDetails_returns_ordered_installments_without_tracking()
    {
        var seeded = await SeedAsync(_context);
        _context.ChangeTracker.Clear();

        var result = await _repository.GetDetailsAsync(seeded.ActiveNew.Id);

        Assert.NotNull(result);
        Assert.Equal("María", result.Client.Name);
        Assert.Equal([1, 2], result.Installments.Select(x => x.Number).ToArray());
        Assert.Empty(_context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetDetailsForClient_returns_only_the_owners_loan_without_tracking()
    {
        var seeded = await SeedAsync(_context);
        _context.ChangeTracker.Clear();

        var ownersResult = await _repository.GetDetailsForClientAsync(
            seeded.ActiveNew.Id,
            seeded.ActiveNew.ClientId);
        var anotherClientsResult = await _repository.GetDetailsForClientAsync(
            seeded.ActiveNew.Id,
            seeded.ActiveOld.ClientId);

        Assert.NotNull(ownersResult);
        Assert.Equal("María", ownersResult.Client.Name);
        Assert.Equal([1, 2], ownersResult.Installments.Select(x => x.Number).ToArray());
        Assert.Null(anotherClientsResult);
        Assert.Empty(_context.ChangeTracker.Entries());
    }

    #endregion

    #region Paged query tests

    [Fact]
    public async Task GetPaged_uses_active_status_by_default_and_orders_newest_first()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetPagedAsync(new PagedRequest());

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(
            [seeded.ActiveNew.Id, seeded.ActiveOld.Id],
            result.Data.Select(loan => loan.Id).ToArray());
        Assert.All(result.Data, loan => Assert.Equal(LoanStatus.Active, loan.Status));
        var loanWithInstallments = result.Data.Single(loan => loan.Id == seeded.ActiveNew.Id);
        Assert.Equal(2, loanWithInstallments.TotalInstallments);
        Assert.Equal(1, loanWithInstallments.PaidInstallments);
        Assert.Equal("María Gómez", loanWithInstallments.ClientFullName);
    }

    [Fact]
    public async Task GetPaged_with_identification_and_no_status_returns_active_then_completed()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetPagedAsync(
            new PagedRequest(),
            clientIdentification: " 111 ");

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(
            [seeded.ActiveNew.Id, seeded.Completed.Id],
            result.Data.Select(loan => loan.Id).ToArray());
    }

    [Fact]
    public async Task GetPaged_filters_by_status_and_applies_pagination()
    {
        var seeded = await SeedAsync(_context);

        var completedResult = await _repository.GetPagedAsync(
            new PagedRequest(),
            status: LoanStatusFilter.Completed);
        var secondActivePage = await _repository.GetPagedAsync(
            new PagedRequest(Page: 2, PageSize: 1));

        Assert.Equal(seeded.Completed.Id, completedResult.Data.Single().Id);
        Assert.Equal(2, secondActivePage.TotalRecords);
        Assert.Equal(2, secondActivePage.Page);
        Assert.Equal(1, secondActivePage.PageSize);
        Assert.Equal(seeded.ActiveOld.Id, secondActivePage.Data.Single().Id);
    }

    [Fact]
    public async Task GetPaged_with_all_statuses_returns_active_loans_before_completed_loans()
    {
        var seeded = await SeedAsync(_context);

        var result = await _repository.GetPagedAsync(
            new PagedRequest(),
            status: LoanStatusFilter.All);

        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(
            [seeded.ActiveNew.Id, seeded.ActiveOld.Id, seeded.Completed.Id],
            result.Data.Select(loan => loan.Id).ToArray());
    }

    [Fact]
    public async Task GetEligibleClients_returns_only_active_clients_without_active_loan()
    {
        await SeedAsync(_context);

        var result = await _repository.GetEligibleClientsPagedAsync(
            new PagedRequest());

        var client = Assert.Single(result.Data);
        Assert.Equal("client-without-loans", client.Id);
        Assert.Equal("333", client.Identification);
        Assert.Equal("Ana Pérez", client.FullName);
    }

    [Fact]
    public async Task GetEligibleClients_allows_client_with_only_completed_loans_and_filters_identification()
    {
        await SeedAsync(_context);
        var completedOnlyClient = CreateUser(
            "client-completed",
            "Laura",
            "Méndez",
            "444",
            Roles.Client);
        _context.Users.Add(completedOnlyClient);
        AddLoan(
            _context,
            completedOnlyClient.Id,
            "100000004",
            LoanStatus.Completed,
            new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));
        await _context.SaveChangesAsync();

        var result = await _repository.GetEligibleClientsPagedAsync(
            new PagedRequest(),
            " 444 ");

        var client = Assert.Single(result.Data);
        Assert.Equal(completedOnlyClient.Id, client.Id);
    }

    [Fact]
    public async Task GetEligibleClients_applies_pagination_and_identification_order()
    {
        await SeedAsync(_context);
        _context.Users.Add(CreateUser(
            "client-eligible-2",
            "Laura",
            "Méndez",
            "444",
            Roles.Client));
        await _context.SaveChangesAsync();

        var result = await _repository.GetEligibleClientsPagedAsync(
            new PagedRequest(Page: 2, PageSize: 1));

        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(2, result.Page);
        Assert.Equal("client-eligible-2", result.Data.Single().Id);
    }

    [Fact]
    public async Task GetEligibleClientById_rejects_active_loan_and_returns_eligible_client()
    {
        await SeedAsync(_context);

        var activeLoanClient = await _repository.GetEligibleClientByIdAsync("client-1");
        var eligibleClient = await _repository.GetEligibleClientByIdAsync(
            "client-without-loans");

        Assert.Null(activeLoanClient);
        Assert.NotNull(eligibleClient);
        Assert.Equal("333", eligibleClient.Identification);
    }

    #endregion

    #region Persistence tests

    [Fact]
    public async Task AddInstallments_adds_all_installments_after_commit()
    {
        var seeded = await SeedAsync(_context);
        var unitOfWork = new UnitOfWork(_context);
        var installment = CreateInstallment(seeded.ActiveOld.Id, 1, new DateOnly(2026, 2, 1));

        await _repository.AddInstallmentsAsync([installment]);
        await unitOfWork.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var persisted = await _context.LoanInstallments
            .AsNoTracking()
            .SingleAsync(x => x.Id == installment.Id);

        Assert.Equal(seeded.ActiveOld.Id, persisted.LoanId);
        Assert.Equal(1, persisted.Number);
    }

    [Fact]
    public async Task AddPayment_adds_payment_after_commit()
    {
        var seeded = await SeedAsync(_context);
        var unitOfWork = new UnitOfWork(_context);
        var payment = new LoanPayment
        {
            LoanId = seeded.ActiveNew.Id,
            SourceAccountId = seeded.SourceAccount.Id,
            EffectiveAmount = 500m,
            ActorUserId = "admin",
            PaidAtUtc = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero),
            OperationId = Guid.NewGuid()
        };
        _context.Entry(payment).Property(x => x.Id).CurrentValue = Guid.NewGuid();

        await _repository.AddPaymentAsync(payment);
        await unitOfWork.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var persisted = await _context.LoanPayments
            .AsNoTracking()
            .SingleAsync(x => x.OperationId == payment.OperationId);

        Assert.Equal(seeded.ActiveNew.Id, persisted.LoanId);
        Assert.Equal(500m, persisted.EffectiveAmount);
    }

    [Fact]
    public async Task GetPaymentByOperationId_returns_payment_with_loan_without_tracking()
    {
        var seeded = await SeedAsync(_context);
        var operationId = Guid.NewGuid();
        var payment = new LoanPayment
        {
            LoanId = seeded.ActiveNew.Id,
            SourceAccountId = seeded.SourceAccount.Id,
            EffectiveAmount = 250m,
            ActorUserId = "admin",
            PaidAtUtc = new DateTimeOffset(2026, 4, 2, 12, 0, 0, TimeSpan.Zero),
            OperationId = operationId
        };
        _context.Entry(payment).Property(x => x.Id).CurrentValue = Guid.NewGuid();
        _context.LoanPayments.Add(payment);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _repository.GetPaymentByOperationIdAsync(operationId);

        Assert.NotNull(result);
        Assert.Equal(seeded.ActiveNew.LoanNumber, result.Loan.LoanNumber);
        Assert.Empty(_context.ChangeTracker.Entries<LoanPayment>());
        Assert.Empty(_context.ChangeTracker.Entries<Loan>());
    }

    [Fact]
    public async Task MarkOverdueInstallments_updates_only_eligible_rows_and_is_idempotent()
    {
        var seeded = await SeedAsync(_context);
        var bankingDate = new DateOnly(2026, 4, 1);
        var modifiedAtUtc = new DateTimeOffset(
            2026,
            4,
            1,
            4,
            5,
            0,
            TimeSpan.Zero);
        var overdue = CreateInstallment(
            seeded.ActiveOld.Id,
            1,
            bankingDate.AddDays(-1));
        var dueToday = CreateInstallment(
            seeded.ActiveOld.Id,
            2,
            bankingDate);
        var paid = CreateInstallment(
            seeded.ActiveOld.Id,
            3,
            bankingDate.AddDays(-2));
        paid.PendingAmount = 0m;
        paid.PaymentStatus = InstallmentPaymentStatus.Paid;
        var alreadyLate = CreateInstallment(
            seeded.ActiveOld.Id,
            4,
            bankingDate.AddDays(-3));
        alreadyLate.IsLate = true;
        var completedLoanInstallment = CreateInstallment(
            seeded.Completed.Id,
            1,
            bankingDate.AddDays(-4));
        _context.LoanInstallments.AddRange(
            overdue,
            dueToday,
            paid,
            alreadyLate,
            completedLoanInstallment);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var updated = await _repository.MarkOverdueInstallmentsAsync(
            bankingDate,
            modifiedAtUtc);
        var repeated = await _repository.MarkOverdueInstallmentsAsync(
            bankingDate,
            modifiedAtUtc.AddMinutes(1));
        var persisted = await _context.LoanInstallments
            .AsNoTracking()
            .Where(installment =>
                installment.Id == overdue.Id
                || installment.Id == dueToday.Id
                || installment.Id == paid.Id
                || installment.Id == alreadyLate.Id
                || installment.Id == completedLoanInstallment.Id)
            .ToDictionaryAsync(installment => installment.Id);

        Assert.Equal(1, updated);
        Assert.Equal(0, repeated);
        Assert.True(persisted[overdue.Id].IsLate);
        Assert.Equal(
            modifiedAtUtc,
            persisted[overdue.Id].LastModifiedAtUtc);
        Assert.False(persisted[dueToday.Id].IsLate);
        Assert.False(persisted[paid.Id].IsLate);
        Assert.True(persisted[alreadyLate.Id].IsLate);
        Assert.False(persisted[completedLoanInstallment.Id].IsLate);
    }

    [Fact]
    public async Task ClearLateFlagFromPaidInstallments_updates_only_paid_rows_and_is_idempotent()
    {
        var seeded = await SeedAsync(_context);
        var modifiedAtUtc = new DateTimeOffset(
            2026,
            4,
            1,
            4,
            5,
            0,
            TimeSpan.Zero);
        var paidActive = CreateInstallment(
            seeded.ActiveOld.Id,
            1,
            new DateOnly(2026, 3, 1));
        paidActive.PendingAmount = 0m;
        paidActive.PaymentStatus = InstallmentPaymentStatus.Paid;
        paidActive.IsLate = true;
        var paidCompleted = CreateInstallment(
            seeded.Completed.Id,
            1,
            new DateOnly(2026, 3, 1));
        paidCompleted.PendingAmount = 0m;
        paidCompleted.PaymentStatus = InstallmentPaymentStatus.Paid;
        paidCompleted.IsLate = true;
        var stillPending = CreateInstallment(
            seeded.ActiveOld.Id,
            2,
            new DateOnly(2026, 3, 1));
        stillPending.IsLate = true;
        _context.LoanInstallments.AddRange(
            paidActive,
            paidCompleted,
            stillPending);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var updated = await _repository.ClearLateFlagFromPaidInstallmentsAsync(
            null,
            modifiedAtUtc,
            null);
        var repeated = await _repository.ClearLateFlagFromPaidInstallmentsAsync(
            null,
            modifiedAtUtc.AddMinutes(1),
            null);
        var persisted = await _context.LoanInstallments
            .AsNoTracking()
            .Where(installment =>
                installment.Id == paidActive.Id
                || installment.Id == paidCompleted.Id
                || installment.Id == stillPending.Id)
            .ToDictionaryAsync(installment => installment.Id);

        Assert.Equal(2, updated);
        Assert.Equal(0, repeated);
        Assert.False(persisted[paidActive.Id].IsLate);
        Assert.False(persisted[paidCompleted.Id].IsLate);
        Assert.True(persisted[stillPending.Id].IsLate);
        Assert.Equal(
            modifiedAtUtc,
            persisted[paidActive.Id].LastModifiedAtUtc);
        Assert.Equal(
            modifiedAtUtc,
            persisted[paidCompleted.Id].LastModifiedAtUtc);
    }

    [Fact]
    public async Task ClearLateFlagFromPaidInstallments_scopes_update_to_requested_loan_and_actor()
    {
        var seeded = await SeedAsync(_context);
        var modifiedAtUtc = new DateTimeOffset(
            2026,
            4,
            1,
            6,
            0,
            0,
            TimeSpan.Zero);
        var paidRequestedLoan = CreateInstallment(
            seeded.ActiveOld.Id,
            1,
            new DateOnly(2026, 3, 1));
        paidRequestedLoan.PendingAmount = 0m;
        paidRequestedLoan.PaymentStatus = InstallmentPaymentStatus.Paid;
        paidRequestedLoan.IsLate = true;
        var paidOtherLoan = CreateInstallment(
            seeded.ActiveNew.Id,
            3,
            new DateOnly(2026, 3, 1));
        paidOtherLoan.PendingAmount = 0m;
        paidOtherLoan.PaymentStatus = InstallmentPaymentStatus.Paid;
        paidOtherLoan.IsLate = true;
        _context.LoanInstallments.AddRange(
            paidRequestedLoan,
            paidOtherLoan);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var updated = await _repository.ClearLateFlagFromPaidInstallmentsAsync(
            seeded.ActiveOld.Id,
            modifiedAtUtc,
            "cashier-1");
        var persisted = await _context.LoanInstallments
            .AsNoTracking()
            .Where(installment =>
                installment.Id == paidRequestedLoan.Id
                || installment.Id == paidOtherLoan.Id)
            .ToDictionaryAsync(installment => installment.Id);

        Assert.Equal(1, updated);
        Assert.False(persisted[paidRequestedLoan.Id].IsLate);
        Assert.Equal(
            "cashier-1",
            persisted[paidRequestedLoan.Id].LastModifiedByUserId);
        Assert.Equal(
            modifiedAtUtc,
            persisted[paidRequestedLoan.Id].LastModifiedAtUtc);
        Assert.True(persisted[paidOtherLoan.Id].IsLate);
    }

    [Fact]
    public async Task Payment_cleanup_repairs_late_flag_marked_after_installment_was_loaded()
    {
        var seeded = await SeedAsync(_context);
        var bankingDate = new DateOnly(2026, 4, 1);
        var functionModifiedAtUtc = new DateTimeOffset(
            2026,
            4,
            1,
            4,
            5,
            0,
            TimeSpan.Zero);
        var paymentModifiedAtUtc = functionModifiedAtUtc.AddHours(2);
        var installment = CreateInstallment(
            seeded.ActiveOld.Id,
            1,
            bankingDate.AddDays(-1));
        _context.LoanInstallments.Add(installment);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        await using var paymentContext = CreateContext();
        var paymentRepository = new LoanRepository(paymentContext);
        var trackedLoan = await paymentRepository.GetWithInstallmentsAsync(
            seeded.ActiveOld.Id);
        Assert.NotNull(trackedLoan);
        var trackedInstallment = Assert.Single(trackedLoan.Installments);
        Assert.False(trackedInstallment.IsLate);

        var marked = await _repository.MarkOverdueInstallmentsAsync(
            bankingDate,
            functionModifiedAtUtc);
        Assert.Equal(1, marked);

        int cleared;

        using (var transaction = new TransactionScope(
                   TransactionScopeOption.Required,
                   new TransactionOptions
                   {
                       IsolationLevel = IsolationLevel.ReadCommitted
                   },
                   TransactionScopeAsyncFlowOption.Enabled))
        {
            trackedInstallment.PendingAmount = 0m;
            trackedInstallment.PaymentStatus = InstallmentPaymentStatus.Paid;
            trackedInstallment.IsLate = false;
            trackedLoan.PendingAmount = 0m;
            trackedLoan.Status = LoanStatus.Completed;
            await paymentContext.SaveChangesAsync();

            var staleLateFlag = await paymentContext.LoanInstallments
                .AsNoTracking()
                .Where(item => item.Id == trackedInstallment.Id)
                .Select(item => item.IsLate)
                .SingleAsync();
            Assert.True(staleLateFlag);

            cleared = await paymentRepository.ClearLateFlagFromPaidInstallmentsAsync(
                trackedLoan.Id,
                paymentModifiedAtUtc,
                "client-2");
            transaction.Complete();
        }

        await using var verificationContext = CreateContext();
        var persisted = await verificationContext.LoanInstallments
            .AsNoTracking()
            .SingleAsync(item => item.Id == trackedInstallment.Id);

        Assert.Equal(1, cleared);
        Assert.Equal(0m, persisted.PendingAmount);
        Assert.Equal(InstallmentPaymentStatus.Paid, persisted.PaymentStatus);
        Assert.False(persisted.IsLate);
        Assert.Equal(paymentModifiedAtUtc, persisted.LastModifiedAtUtc);
        Assert.Equal("client-2", persisted.LastModifiedByUserId);
    }

    #endregion

    #region Test data builders

    private AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new AppDbContext(options);
    }

    private static async Task<SeededLoans> SeedAsync(AppDbContext context)
    {
        var admin = CreateUser("admin", "Admin", "System", "000", Roles.Administrator);
        var firstClient = CreateUser("client-1", "María", "Gómez", "111", Roles.Client);
        var secondClient = CreateUser("client-2", "Pedro", "Díaz", "222", Roles.Client);
        var clientWithoutLoans = CreateUser(
            "client-without-loans",
            "Ana",
            "Pérez",
            "333",
            Roles.Client);
        var inactiveClient = CreateUser(
            "client-inactive",
            "José",
            "Ruiz",
            "555",
            Roles.Client);
        inactiveClient.IsActive = false;
        context.Users.AddRange(
            admin,
            firstClient,
            secondClient,
            clientWithoutLoans,
            inactiveClient);

        var sourceAccount = new SavingsAccount(Guid.NewGuid())
        {
            OwnerUserId = firstClient.Id,
            AccountNumber = "200000001",
            Balance = 5_000m,
            Type = SavingsAccountType.Principal,
            Status = SavingsAccountStatus.Active
        };
        context.SavingsAccounts.Add(sourceAccount);

        var activeOld = AddLoan(
            context,
            secondClient.Id,
            "100000001",
            LoanStatus.Active,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var completed = AddLoan(
            context,
            firstClient.Id,
            "100000002",
            LoanStatus.Completed,
            new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero));
        var activeNew = AddLoan(
            context,
            firstClient.Id,
            "100000003",
            LoanStatus.Active,
            new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero));

        await context.SaveChangesAsync();

        var paidInstallment = CreateInstallment(
            activeNew.Id,
            1,
            new DateOnly(2026, 4, 1));
        paidInstallment.PendingAmount = 0m;
        paidInstallment.PaymentStatus = InstallmentPaymentStatus.Paid;

        context.LoanInstallments.AddRange(
            CreateInstallment(activeNew.Id, 2, new DateOnly(2026, 5, 1)),
            paidInstallment);
        await context.SaveChangesAsync();

        return new(activeOld, activeNew, completed, sourceAccount);
    }

    private static User CreateUser(
        string id,
        string name,
        string lastName,
        string identification,
        Roles role) =>
        new(id)
        {
            Name = name,
            LastName = lastName,
            Identification = identification,
            Email = $"{id}@example.test",
            UserName = id,
            IsActive = true,
            Role = role
        };

    private static Loan AddLoan(
        AppDbContext context,
        string clientId,
        string loanNumber,
        LoanStatus status,
        DateTimeOffset createdAt)
    {
        var loan = new Loan
        {
            ClientId = clientId,
            LoanNumber = loanNumber,
            Capital = 10_000m,
            PendingAmount = status == LoanStatus.Active ? 10_000m : 0m,
            AnnualInterestRate = 12m,
            TermInMonths = 12,
            Status = status,
            AssignedByUserId = "admin"
        };

        context.Loans.Add(loan);
        context.Entry(loan).Property(x => x.Id).CurrentValue = Guid.NewGuid();
        context.Entry(loan).Property(x => x.CreatedAtUtc).CurrentValue = createdAt;
        return loan;
    }

    private static LoanInstallment CreateInstallment(
        Guid loanId,
        int number,
        DateOnly dueDate)
    {
        return new LoanInstallment
        {
            LoanId = loanId,
            Number = number,
            DueDate = dueDate,
            InstallmentAmount = 900m,
            InterestAmount = 100m,
            CapitalAmount = 800m,
            PendingAmount = 900m,
            PaymentStatus = InstallmentPaymentStatus.Pending,
            IsLate = false
        };
    }

    private sealed record SeededLoans(
        Loan ActiveOld,
        Loan ActiveNew,
        Loan Completed,
        SavingsAccount SourceAccount);

    #endregion
}
