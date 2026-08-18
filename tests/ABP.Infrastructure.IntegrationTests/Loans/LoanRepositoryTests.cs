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
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        _context = new AppDbContext(options);
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

    #endregion

    #region Test data builders

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
