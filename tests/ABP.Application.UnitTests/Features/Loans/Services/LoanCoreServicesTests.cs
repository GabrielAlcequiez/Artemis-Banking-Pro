using ABP.Application.Common.Interfaces.Services;
using ABP.Application.Features.Loans;
using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Services.Implementations;
using ABP.Application.Features.Loans.Services.Interfaces;
using ABP.Application.Features.Loans.Validation;
using ABP.Application.UnitTests.Features.Loans;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Loans.Services;

public sealed class LoanCoreServicesTests
{
    #region Loan rate service tests

    [Fact]
    public async Task Update_rate_returns_not_found_without_recalculating_or_committing()
    {
        var repository = new FakeLoanRepository();
        var calculator = new FakeAmortizationCalculator();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateRateService(repository, calculator, unitOfWork);

        var result = await service.UpdateRateAsync(
            new UpdateLoanRateRequest(Guid.NewGuid(), 9.5m));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.NotFound, result.Error);
        Assert.Equal(0, calculator.CalculateCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Update_rate_rejects_completed_loan_without_committing()
    {
        var repository = new FakeLoanRepository
        {
            LoanWithInstallments = CreateLoan(LoanStatus.Completed)
        };
        var calculator = new FakeAmortizationCalculator();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateRateService(repository, calculator, unitOfWork);

        var result = await service.UpdateRateAsync(
            new UpdateLoanRateRequest(Guid.NewGuid(), 9.5m));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.Inactive, result.Error);
        Assert.Equal(0, calculator.CalculateCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Update_rate_rejects_when_no_eligible_future_installments_exist()
    {
        var today = new DateOnly(2026, 8, 10);
        var loan = CreateLoan();
        loan.Installments =
        [
            CreateInstallment(1, today.AddDays(-1), InstallmentPaymentStatus.Pending, 50m),
            CreateInstallment(2, today, InstallmentPaymentStatus.Pending, 50m),
            CreateInstallment(3, today.AddMonths(1), InstallmentPaymentStatus.PartiallyPaid, 25m),
            CreateInstallment(4, today.AddMonths(2), InstallmentPaymentStatus.Pending, 50m, isLate: true),
            CreateInstallment(5, today.AddMonths(3), InstallmentPaymentStatus.Paid, 0m)
        ];
        var repository = new FakeLoanRepository { LoanWithInstallments = loan };
        var calculator = new FakeAmortizationCalculator();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateRateService(repository, calculator, unitOfWork, today);

        var result = await service.UpdateRateAsync(
            new UpdateLoanRateRequest(Guid.NewGuid(), 8m));

        Assert.True(result.IsFailure);
        Assert.Equal(LoanErrors.NoFuturePendingInstallments, result.Error);
        Assert.Equal(0, calculator.CalculateCalls);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Update_rate_recalculates_only_eligible_installments_and_commits_once()
    {
        var today = new DateOnly(2026, 8, 10);
        var paid = CreateInstallment(1, today.AddMonths(-1), InstallmentPaymentStatus.Paid, 0m, capitalAmount: 40m);
        var partial = CreateInstallment(2, today.AddMonths(1), InstallmentPaymentStatus.PartiallyPaid, 30m, capitalAmount: 60m);
        var firstEligible = CreateInstallment(3, today.AddMonths(2), InstallmentPaymentStatus.Pending, 70m, capitalAmount: 70m);
        var secondEligible = CreateInstallment(4, today.AddMonths(3), InstallmentPaymentStatus.Pending, 80m, capitalAmount: 80m);
        var late = CreateInstallment(5, today.AddMonths(4), InstallmentPaymentStatus.Pending, 40m, isLate: true, capitalAmount: 40m);
        var loan = CreateLoan();
        loan.Installments = [paid, partial, firstEligible, secondEligible, late];

        var repository = new FakeLoanRepository { LoanWithInstallments = loan };
        var calculator = new FakeAmortizationCalculator
        {
            Result = new AmortizationResult(
                90m,
                181m,
                [
                    new LoanInstallmentDto(1, today.AddMonths(1), 90m, 10m, 80m, 90m, "Pendiente", false),
                    new LoanInstallmentDto(2, today.AddMonths(2), 91m, 11m, 70m, 91m, "Pendiente", false)
                ])
        };
        var unitOfWork = new FakeUnitOfWork();
        var emails = new RecordingLoanEmailService
        {
            IsOperationCommitted = () => unitOfWork.SaveCalls > 0
        };
        var service = CreateRateService(
            repository,
            calculator,
            unitOfWork,
            today,
            emails);

        var result = await service.UpdateRateAsync(
            new UpdateLoanRateRequest(Guid.NewGuid(), 7.25m));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, calculator.CalculateCalls);
        Assert.Equal(150m, calculator.ReceivedCapital);
        Assert.Equal(7.25m, calculator.ReceivedAnnualInterestRate);
        Assert.Equal(2, calculator.ReceivedTermInMonths);
        Assert.Equal(today, calculator.ReceivedCreationDate);
        Assert.Equal(90m, firstEligible.InstallmentAmount);
        Assert.Equal(10m, firstEligible.InterestAmount);
        Assert.Equal(80m, firstEligible.CapitalAmount);
        Assert.Equal(90m, firstEligible.PendingAmount);
        Assert.Equal(91m, secondEligible.InstallmentAmount);
        Assert.Equal(30m, partial.PendingAmount);
        Assert.Equal(40m, late.PendingAmount);
        Assert.Equal(7.25m, loan.AnnualInterestRate);
        Assert.Equal(251m, loan.PendingAmount);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.False(result.HasNotificationWarning);
        var email = Assert.Single(emails.SentEmails);
        Assert.Equal("client@example.com", email.ToEmail);
        Assert.Equal(
            "Actualización de tasa de interés de préstamo",
            email.Subject);
        Assert.Contains("90.00", email.Body);
        Assert.False(emails.WasCalledBeforeCommit);
    }

    [Fact]
    public async Task Update_rate_keeps_changes_when_email_fails()
    {
        var today = new DateOnly(2026, 8, 10);
        var loan = CreateLoan();
        loan.Installments =
        [
            CreateInstallment(
                1,
                today.AddMonths(1),
                InstallmentPaymentStatus.Pending,
                100m,
                capitalAmount: 90m)
        ];
        var repository = new FakeLoanRepository { LoanWithInstallments = loan };
        var calculator = new FakeAmortizationCalculator
        {
            Result = new AmortizationResult(
                100m,
                100m,
                [new LoanInstallmentDto(1, today.AddMonths(1), 100m, 10m, 90m, 100m, "Pendiente", false)])
        };
        var unitOfWork = new FakeUnitOfWork();
        var emails = new RecordingLoanEmailService
        {
            ThrowOnSend = true,
            IsOperationCommitted = () => unitOfWork.SaveCalls > 0
        };
        var service = CreateRateService(
            repository,
            calculator,
            unitOfWork,
            today,
            emails);

        var result = await service.UpdateRateAsync(
            new UpdateLoanRateRequest(Guid.NewGuid(), 8m));

        Assert.True(result.IsSuccess);
        Assert.True(result.HasNotificationWarning);
        Assert.Equal(8m, loan.AnnualInterestRate);
        Assert.Equal(1, unitOfWork.SaveCalls);
        Assert.Equal(1, emails.SendAttempts);
        Assert.False(emails.WasCalledBeforeCommit);
    }

    [Fact]
    public async Task Update_rate_uses_validator_before_querying_repository()
    {
        var repository = new FakeLoanRepository();
        var service = CreateRateService(
            repository,
            new FakeAmortizationCalculator(),
            new FakeUnitOfWork());

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.UpdateRateAsync(new UpdateLoanRateRequest(Guid.NewGuid(), -0.01m)));

        Assert.Equal(0, repository.GetWithInstallmentsCalls);
    }

    #endregion

    #region Loan delinquency service tests

    [Fact]
    public async Task Delinquency_returns_zero_when_repository_updates_nothing()
    {
        var bankingDate = new DateOnly(2026, 8, 10);
        var repository = new FakeLoanRepository();
        var service = CreateDelinquencyService(repository, bankingDate);

        var updated = await service.UpdateDelinquencyAsync(bankingDate);

        Assert.Equal(0, updated);
        Assert.Equal(1, repository.MarkOverdueCalls);
        Assert.Equal(1, repository.ClearPaidLateCalls);
        Assert.Equal(bankingDate, repository.ReceivedBankingDate);
        Assert.Equal(
            new DateTimeOffset(2026, 8, 10, 12, 0, 0, TimeSpan.Zero),
            repository.ReceivedMarkModifiedAtUtc);
        Assert.Equal(
            repository.ReceivedMarkModifiedAtUtc,
            repository.ReceivedClearModifiedAtUtc);
        Assert.Null(repository.ReceivedClearLoanId);
        Assert.Null(repository.ReceivedClearModifiedByUserId);
    }

    [Fact]
    public async Task Delinquency_returns_total_updated_by_both_repository_operations()
    {
        var bankingDate = new DateOnly(2026, 8, 10);
        var repository = new FakeLoanRepository
        {
            MarkedOverdueCount = 3,
            ClearedPaidLateCount = 2
        };
        var service = CreateDelinquencyService(repository, bankingDate);

        var updated = await service.UpdateDelinquencyAsync(bankingDate);

        Assert.Equal(5, updated);
        Assert.Equal(1, repository.MarkOverdueCalls);
        Assert.Equal(1, repository.ClearPaidLateCalls);
        Assert.Equal(bankingDate, repository.ReceivedBankingDate);
    }

    [Fact]
    public async Task Delinquency_propagates_cancellation_token_to_both_updates()
    {
        var bankingDate = new DateOnly(2026, 8, 10);
        var repository = new FakeLoanRepository();
        var service = CreateDelinquencyService(repository, bankingDate);
        using var cancellationSource = new CancellationTokenSource();

        await service.UpdateDelinquencyAsync(
            bankingDate,
            cancellationSource.Token);

        Assert.Equal(
            cancellationSource.Token,
            repository.MarkOverdueCancellationToken);
        Assert.Equal(
            cancellationSource.Token,
            repository.ClearPaidLateCancellationToken);
    }

    [Fact]
    public async Task Delinquency_does_not_clear_flags_when_marking_fails()
    {
        var bankingDate = new DateOnly(2026, 8, 10);
        var repository = new FakeLoanRepository
        {
            MarkOverdueException = new InvalidOperationException(
                "No fue posible actualizar las cuotas vencidas.")
        };
        var service = CreateDelinquencyService(repository, bankingDate);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateDelinquencyAsync(bankingDate));

        Assert.Equal(1, repository.MarkOverdueCalls);
        Assert.Equal(0, repository.ClearPaidLateCalls);
    }

    #endregion

    #region Loan metrics reader tests

    [Fact]
    public async Task Count_active_loans_delegates_to_repository()
    {
        var repository = new FakeLoanRepository { ActiveLoanCount = 7 };
        var reader = new LoansMetricsReader(repository);
        using var cancellationSource = new CancellationTokenSource();

        var result = await reader.CountActiveLoansAsync(
            cancellationSource.Token);

        Assert.Equal(7, result);
        Assert.Equal(1, repository.CountActiveLoansCalls);
        Assert.Equal(
            cancellationSource.Token,
            repository.MetricsCancellationToken);
    }

    #endregion

    #region Test helpers

    private static ILoanRateService CreateRateService(
        FakeLoanRepository repository,
        FakeAmortizationCalculator calculator,
        FakeUnitOfWork unitOfWork,
        DateOnly? today = null,
        RecordingLoanEmailService? emails = null) =>
        new LoanRateService(
            repository,
            calculator,
            unitOfWork,
            new FakeClock(today ?? new DateOnly(2026, 8, 10)),
            new UpdateLoanRateRequestValidator(),
            emails ?? new RecordingLoanEmailService(),
            NullLogger<LoanRateService>.Instance);

    private static ILoanDelinquencyService CreateDelinquencyService(
        FakeLoanRepository repository,
        DateOnly bankingDate) =>
        new LoanDelinquencyService(
            repository,
            new FakeClock(bankingDate),
            NullLogger<LoanDelinquencyService>.Instance);

    private static Loan CreateLoan(LoanStatus status = LoanStatus.Active) => new()
    {
        ClientId = "client-1",
        LoanNumber = "123456789",
        Status = status,
        AnnualInterestRate = 12m,
        PendingAmount = 190m,
        Client = new User("client-1")
        {
            Name = "Ana",
            LastName = "Pérez",
            Email = "client@example.com"
        }
    };

    private static LoanInstallment CreateInstallment(
        int number,
        DateOnly dueDate,
        InstallmentPaymentStatus paymentStatus,
        decimal pendingAmount,
        bool isLate = false,
        decimal capitalAmount = 50m) => new()
    {
        Number = number,
        DueDate = dueDate,
        InstallmentAmount = pendingAmount,
        InterestAmount = 0m,
        CapitalAmount = capitalAmount,
        PendingAmount = pendingAmount,
        PaymentStatus = paymentStatus,
        IsLate = isLate
    };

    private sealed class FakeLoanRepository : ILoanRepository
    {
        public Loan? LoanWithInstallments { get; init; }

        public int MarkedOverdueCount { get; init; }

        public int ClearedPaidLateCount { get; init; }

        public Exception? MarkOverdueException { get; init; }

        public int ActiveLoanCount { get; init; }

        public int GetWithInstallmentsCalls { get; private set; }

        public int CountActiveLoansCalls { get; private set; }

        public int MarkOverdueCalls { get; private set; }

        public int ClearPaidLateCalls { get; private set; }

        public DateOnly? ReceivedBankingDate { get; private set; }

        public DateTimeOffset? ReceivedMarkModifiedAtUtc { get; private set; }

        public DateTimeOffset? ReceivedClearModifiedAtUtc { get; private set; }

        public Guid? ReceivedClearLoanId { get; private set; }

        public string? ReceivedClearModifiedByUserId { get; private set; }

        public CancellationToken MarkOverdueCancellationToken { get; private set; }

        public CancellationToken ClearPaidLateCancellationToken { get; private set; }

        public CancellationToken MetricsCancellationToken { get; private set; }

        public Task<Loan?> GetWithInstallmentsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetWithInstallmentsCalls++;
            return Task.FromResult(LoanWithInstallments);
        }

        public Task<Loan?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<Loan?> GetDetailsForClientAsync(Guid id, string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
        public Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<decimal> GetActiveDebtByClientIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyDictionary<string, decimal>> GetActiveDebtByClientIdsAsync(IReadOnlyCollection<string> clientIds, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<decimal> GetTotalActiveDebtForActiveClientsAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResult<LoanClientCandidateReadModel>> GetEligibleClientsPagedAsync(PagedRequest request, string? clientIdentification = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LoanClientCandidateReadModel?> GetEligibleClientByIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<int> CountActiveLoansAsync(CancellationToken cancellationToken = default)
        {
            CountActiveLoansCalls++;
            MetricsCancellationToken = cancellationToken;
            return Task.FromResult(ActiveLoanCount);
        }

        public Task<int> MarkOverdueInstallmentsAsync(
            DateOnly bankingDate,
            DateTimeOffset modifiedAtUtc,
            CancellationToken cancellationToken = default)
        {
            MarkOverdueCalls++;
            ReceivedBankingDate = bankingDate;
            ReceivedMarkModifiedAtUtc = modifiedAtUtc;
            MarkOverdueCancellationToken = cancellationToken;

            if (MarkOverdueException is not null)
            {
                throw MarkOverdueException;
            }

            return Task.FromResult(MarkedOverdueCount);
        }

        public Task<int> ClearLateFlagFromPaidInstallmentsAsync(
            Guid? loanId,
            DateTimeOffset modifiedAtUtc,
            string? modifiedByUserId,
            CancellationToken cancellationToken = default)
        {
            ClearPaidLateCalls++;
            ReceivedClearLoanId = loanId;
            ReceivedClearModifiedAtUtc = modifiedAtUtc;
            ReceivedClearModifiedByUserId = modifiedByUserId;
            ClearPaidLateCancellationToken = cancellationToken;
            return Task.FromResult(ClearedPaidLateCount);
        }

        public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<ClientLoanPortfolioReadModel?> GetActivePortfolioForClientAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> HasActiveLoanAsync(string clientId, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> LoanNumberExistsAsync(string loanNumber, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(PagedRequest request, string? clientIdentification = null, LoanStatusFilter? status = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddInstallmentsAsync(IReadOnlyCollection<LoanInstallment> installments, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public IQueryable<Loan> GetAllQueryable(bool trackChanges = false) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<Loan>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan> AddAsync(Loan entity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> UpdateAsync(Guid id, Loan value, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<Loan?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeAmortizationCalculator : IAmortizationCalculator
    {
        public AmortizationResult Result { get; init; } = new(0m, 0m, []);

        public int CalculateCalls { get; private set; }

        public decimal ReceivedCapital { get; private set; }

        public decimal ReceivedAnnualInterestRate { get; private set; }

        public int ReceivedTermInMonths { get; private set; }

        public DateOnly ReceivedCreationDate { get; private set; }

        public AmortizationResult Calculate(
            decimal capital,
            decimal annualInterestRate,
            int termInMonths,
            DateOnly creationDate)
        {
            CalculateCalls++;
            ReceivedCapital = capital;
            ReceivedAnnualInterestRate = annualInterestRate;
            ReceivedTermInMonths = termInMonths;
            ReceivedCreationDate = creationDate;
            return Result;
        }
    }

    private sealed class FakeClock(DateOnly today) : IClock
    {
        public DateTimeOffset UtcNow => new(
            today.Year,
            today.Month,
            today.Day,
            12,
            0,
            0,
            TimeSpan.Zero);

        public DateTimeOffset Now => UtcNow;

        public DateOnly Today => today;
    }

    #endregion
}
