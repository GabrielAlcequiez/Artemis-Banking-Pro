using ABP.Application.Features.Loans.Mapping;
using ABP.Application.Features.Loans.Queries.GetLoanDetail;
using ABP.Domain.Common;
using ABP.Domain.Entities;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Loans.Queries.GetLoanDetail;

public sealed class GetLoanDetailQueryTests
{
    [Fact]
    public async Task Validator_rejects_empty_loan_id_with_spanish_message()
    {
        var validator = new GetLoanDetailQueryValidator();

        var result = await validator.ValidateAsync(
            new GetLoanDetailQuery(Guid.Empty));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal(nameof(GetLoanDetailQuery.LoanId), error.PropertyName);
        Assert.Equal(
            "El identificador del préstamo es obligatorio.",
            error.ErrorMessage);
    }

    [Fact]
    public async Task Handler_with_existing_loan_maps_complete_detail()
    {
        var loanId = Guid.NewGuid();
        var repository = new StubLoanRepository
        {
            Detail = CreateLoan()
        };
        var handler = new GetLoanDetailQueryHandler(
            repository,
            CreateMapper());
        using var cancellationSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new GetLoanDetailQuery(loanId),
            cancellationSource.Token);

        Assert.NotNull(result);
        Assert.Equal(loanId, repository.ReceivedLoanId);
        Assert.Equal(
            cancellationSource.Token,
            repository.ReceivedCancellationToken);
        Assert.Equal("123456789", result.LoanNumber);
        Assert.Equal("Ana Pérez", result.ClientFullName);
        Assert.Equal(1_000m, result.CapitalAmount);
        Assert.Equal("Activo", result.Status);
        Assert.Equal("Al día", result.ClientPaymentStatus);
        Assert.Equal([1, 2], result.Amortization.Select(
            installment => installment.InstallmentNumber));
    }

    [Fact]
    public async Task Handler_with_missing_loan_returns_null()
    {
        var loanId = Guid.NewGuid();
        var repository = new StubLoanRepository();
        var handler = new GetLoanDetailQueryHandler(
            repository,
            CreateMapper());

        var result = await handler.Handle(
            new GetLoanDetailQuery(loanId),
            CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(loanId, repository.ReceivedLoanId);
    }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            configuration => configuration.AddProfile<LoanProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

    private static Loan CreateLoan() => new()
    {
        ClientId = "client-1",
        Client = new User("client-1")
        {
            Name = "Ana",
            LastName = "Pérez"
        },
        LoanNumber = "123456789",
        Capital = 1_000m,
        PendingAmount = 750m,
        AnnualInterestRate = 12m,
        TermInMonths = 12,
        Status = LoanStatus.Active,
        AssignedByUserId = "admin-1",
        Installments =
        [
            CreateInstallment(2),
            CreateInstallment(1)
        ]
    };

    private static LoanInstallment CreateInstallment(int number) => new()
    {
        Number = number,
        DueDate = new DateOnly(2026, 9, 10).AddMonths(number - 1),
        InstallmentAmount = 100m,
        InterestAmount = 10m,
        CapitalAmount = 90m,
        PendingAmount = 100m,
        PaymentStatus = InstallmentPaymentStatus.Pending
    };

    private sealed class StubLoanRepository : ILoanRepository
    {
        public Loan? Detail { get; init; }

        public Guid? ReceivedLoanId { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<Loan?> GetDetailsAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            ReceivedLoanId = id;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(Detail);
        }

        public Task<Loan?> GetDetailsForClientAsync(
            Guid id,
            string clientId,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();

        public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetActiveByClientIdAsync(string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
        public Task AddPaymentAsync(LoanPayment payment, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IQueryable<Loan> GetAllQueryable(bool trackChanges = false) => throw new NotImplementedException();
        public Task<IReadOnlyList<Loan>> GetAllAsync(bool trackChanges = false, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan> AddAsync(Loan entity, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> UpdateAsync(Guid id, Loan value, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> DeleteAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
