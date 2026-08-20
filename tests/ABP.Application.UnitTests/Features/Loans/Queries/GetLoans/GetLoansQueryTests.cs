using ABP.Application.Features.Loans.DTOs;
using ABP.Application.Features.Loans.Mapping;
using ABP.Application.Features.Loans.Queries.GetLoans;
using ABP.Application.Features.Loans.Validation;
using ABP.Domain.Common;
using ABP.Domain.Entities.Lending;
using ABP.Domain.Enums;
using ABP.Domain.Interfaces;
using ABP.Domain.ReadModels.Loans;
using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;

namespace ABP.Application.UnitTests.Features.Loans.Queries.GetLoans;

public sealed class GetLoansQueryTests
{
    [Fact]
    public async Task Validator_reuses_shared_list_request_rules()
    {
        var validator = new GetLoansQueryValidator(
            new LoanListRequestValidator());
        var query = new GetLoansQuery(
            new LoanListRequest(PageSize: 21));

        var result = await validator.ValidateAsync(query);

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Errors,
            error => error.PropertyName == "Request.PageSize");
    }

    [Fact]
    public async Task Handler_normalizes_filters_and_maps_paged_summary()
    {
        var repository = new StubLoanRepository
        {
            Page = new PagedResult<LoanSummaryReadModel>(
                [CreateSummary()],
                2,
                5,
                8)
        };
        var handler = new GetLoansQueryHandler(repository, CreateMapper());
        using var cancellationSource = new CancellationTokenSource();

        var result = await handler.Handle(
            new GetLoansQuery(
                new LoanListRequest(
                    2,
                    5,
                    " 00100000001 ",
                    LoanStatusFilter.All)),
            cancellationSource.Token);

        Assert.Equal(2, repository.ReceivedRequest?.Page);
        Assert.Equal(5, repository.ReceivedRequest?.PageSize);
        Assert.Equal("00100000001", repository.ReceivedIdentification);
        Assert.Equal(LoanStatusFilter.All, repository.ReceivedStatus);
        Assert.Equal(cancellationSource.Token, repository.ReceivedCancellationToken);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
        Assert.Equal(8, result.TotalRecords);
        var loan = Assert.Single(result.Data);
        Assert.Equal("123456789", loan.LoanNumber);
        Assert.Equal("Ana Pérez", loan.ClientFullName);
        Assert.Equal(12, loan.TotalInstallments);
        Assert.Equal(4, loan.PaidInstallments);
        Assert.Equal("Activo", loan.Status);
        Assert.Equal("En mora", loan.ClientPaymentStatus);
    }

    [Fact]
    public async Task Handler_converts_blank_identification_to_null()
    {
        var repository = new StubLoanRepository();
        var handler = new GetLoansQueryHandler(repository, CreateMapper());

        await handler.Handle(
            new GetLoansQuery(
                new LoanListRequest(Identification: "   ")),
            CancellationToken.None);

        Assert.Null(repository.ReceivedIdentification);
    }

    private static IMapper CreateMapper() =>
        new MapperConfiguration(
            configuration => configuration.AddProfile<LoanProfile>(),
            NullLoggerFactory.Instance).CreateMapper();

    private static LoanSummaryReadModel CreateSummary() =>
        new(
            Guid.NewGuid(),
            "123456789",
            "client-1",
            "Ana Pérez",
            1_000m,
            12,
            4,
            600m,
            12m,
            12,
            LoanStatus.Active,
            true,
            new DateTimeOffset(2026, 8, 15, 12, 0, 0, TimeSpan.Zero));

    private sealed class StubLoanRepository : ILoanRepository
    {
        public PagedResult<LoanSummaryReadModel> Page { get; init; } =
            new([], 1, 20, 0);

        public PagedRequest? ReceivedRequest { get; private set; }

        public string? ReceivedIdentification { get; private set; }

        public LoanStatusFilter? ReceivedStatus { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task<PagedResult<LoanSummaryReadModel>> GetPagedAsync(
            PagedRequest request,
            string? clientIdentification = null,
            LoanStatusFilter? status = null,
            CancellationToken cancellationToken = default)
        {
            ReceivedRequest = request;
            ReceivedIdentification = clientIdentification;
            ReceivedStatus = status;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(Page);
        }

        public Task<Loan?> GetByLoanNumberAsync(string loanNumber, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetWithInstallmentsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<Loan?> GetDetailsForClientAsync(Guid id, string clientId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<LoanPayment?> GetPaymentByOperationIdAsync(Guid operationId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
        public Task<int> MarkOverdueInstallmentsAsync(DateOnly bankingDate, DateTimeOffset modifiedAtUtc, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<int> ClearLateFlagFromPaidInstallmentsAsync(Guid? loanId, DateTimeOffset modifiedAtUtc, string? modifiedByUserId, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
